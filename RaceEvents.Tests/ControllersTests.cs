using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using RaceEvents.Controllers;
using RaceEvents.Data;
using RaceEvents.Models;
using RaceEvents.Models.Enums;
using RaceEvents.Models.ViewModels;
using Xunit;

namespace RaceEvents.Tests;

public class ControllersTests
{
    // Создание in-memory базы данных для изоляции тестов
    // Каждый тест получает чистую базу данных без влияния других тестов
    private ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    // Создание контроллера ApplicationsController с настроенным контекстом и сессией
    // Мокирует HTTP-сессию для имитации авторизованного пользователя-участника
    private ApplicationsController CreateApplicationsController(ApplicationDbContext context, int userId, string role = "PARTICIPANT")
    {
        var httpContext = new DefaultHttpContext();
        var session = new MockHttpSession();
        session.SetInt32("UserId", userId);
        session.SetString("UserRole", role);
        httpContext.Session = session;
        
        var tempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
        
        var controller = new ApplicationsController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = tempData
        };

        return controller;
    }

    // Создание контроллера ApplicationsAdminController для тестирования административных функций
    private ApplicationsAdminController CreateApplicationsAdminController(ApplicationDbContext context, int userId, string role = "ADMINISTRATOR")
    {
        var httpContext = new DefaultHttpContext();
        var session = new MockHttpSession();
        session.SetInt32("UserId", userId);
        session.SetString("UserRole", role);
        httpContext.Session = session;
        
        var tempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
        
        var controller = new ApplicationsAdminController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = tempData
        };

        return controller;
    }

    // Создание контроллера LapTimesController для тестирования записи времени кругов
    private LapTimesController CreateLapTimesController(ApplicationDbContext context, int userId, string role = "ADMINISTRATOR")
    {
        var httpContext = new DefaultHttpContext();
        var session = new MockHttpSession();
        session.SetInt32("UserId", userId);
        session.SetString("UserRole", role);
        httpContext.Session = session;
        
        var tempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
        
        var controller = new LapTimesController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = tempData
        };

        return controller;
    }

    // ТЕСТ 1: Создание заявки участником
    // Проверяет основную функциональность - успешное создание заявки на участие в событии
    // Это ключевой сценарий использования системы участниками
    [Fact]
    public async Task ApplicationsController_Create_POST_CreatesApplication_WhenValid()
    {
        var context = CreateDbContext();
        
        // Подготовка тестовых данных: участник, администратор, автомобиль, событие
        var participant = new Participant
        {
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "User",
            Role = Role.PARTICIPANT,
            PasswordHash = "hash"
        };
        context.Participants.Add(participant);

        var admin = new Administrator
        {
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User",
            Role = Role.ADMINISTRATOR,
            PasswordHash = "hash"
        };
        context.Administrators.Add(admin);

        // Автомобиль класса A, который соответствует требованиям события
        var car = new Car
        {
            ParticipantId = participant.Id,
            Brand = "Toyota",
            Model = "Corolla",
            CarClass = "A",
            LicensePlate = "ABC123",
            Horsepower = 150
        };
        context.Cars.Add(car);

        // Событие с открытой регистрацией и без строгих требований к классу автомобиля
        var eventItem = new Event
        {
            Title = "Test Event",
            Date = DateTime.Now.AddDays(1),
            Location = "Test Location",
            AdministratorId = admin.Id,
            Status = EventStatus.REGISTRATION_OPEN,
            MaxParticipants = 100,
            CarTypeRequirement = CarTypeRequirement.ANY
        };
        context.Events.Add(eventItem);

        await context.SaveChangesAsync();

        // Получение сохраненных сущностей с установленными ID
        var carFromDb = await context.Cars.FirstOrDefaultAsync(c => c.ParticipantId == participant.Id);
        Assert.NotNull(carFromDb);
        Assert.True(carFromDb.Id > 0, "Car ID should be set after SaveChangesAsync");
        
        var eventFromDb = await context.Events.FirstOrDefaultAsync(e => e.Id == eventItem.Id);
        Assert.NotNull(eventFromDb);
        Assert.True(eventFromDb.Id > 0, "Event ID should be set after SaveChangesAsync");

        var controller = CreateApplicationsController(context, participant.Id);
        
        // Создание модели заявки с валидными данными
        var model = new ApplicationViewModel
        {
            EventId = eventFromDb.Id,
            CarId = carFromDb.Id,
            HelmetType = HelmetType.OWN,
            TimerType = TimerType.NONE
        };
        
        var result = await controller.Create(model);

        // Проверка ошибок валидации (если есть)
        if (result is ViewResult viewResult)
        {
            var errors = string.Join(", ", controller.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            Assert.Fail($"ModelState is invalid. Errors: {errors}");
        }

        // Успешное создание должно привести к редиректу на страницу списка заявок
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);

        // Проверка, что заявка создана в базе данных со статусом "Ожидает рассмотрения"
        var application = await context.Applications.FirstOrDefaultAsync();
        Assert.NotNull(application);
        Assert.Equal(ApplicationStatus.PENDING, application.Status);
    }

    // ТЕСТ 2: Валидация бизнес-правил при создании заявки
    // Проверяет, что система корректно отклоняет заявки, когда автомобиль не соответствует требованиям события
    // Это критически важно для обеспечения соблюдения правил соревнований
    [Fact]
    public async Task ApplicationsController_Create_POST_ReturnsView_WhenCarDoesNotMatchEventRequirements()
    {
        var context = CreateDbContext();
        var participant = new Participant
        {
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "User",
            Role = Role.PARTICIPANT,
            PasswordHash = "hash"
        };
        context.Participants.Add(participant);

        var admin = new Administrator
        {
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User",
            Role = Role.ADMINISTRATOR,
            PasswordHash = "hash"
        };
        context.Administrators.Add(admin);

        // Автомобиль класса B, который НЕ соответствует требованию события (требуется класс A)
        var car = new Car
        {
            ParticipantId = participant.Id,
            Brand = "Toyota",
            Model = "Corolla",
            CarClass = "B",
            LicensePlate = "ABC123"
        };
        context.Cars.Add(car);

        // Событие с требованием конкретного класса автомобиля (класс A)
        var eventItem = new Event
        {
            Title = "Test Event",
            Date = DateTime.Now.AddDays(1),
            Location = "Test Location",
            AdministratorId = admin.Id,
            Status = EventStatus.REGISTRATION_OPEN,
            MaxParticipants = 100,
            CarTypeRequirement = CarTypeRequirement.SPECIFIC_CLASS,
            RequiredCarClass = "A"
        };
        context.Events.Add(eventItem);

        await context.SaveChangesAsync();

        var controller = CreateApplicationsController(context, participant.Id);
        var model = new ApplicationViewModel
        {
            EventId = eventItem.Id,
            CarId = car.Id,
            HelmetType = HelmetType.OWN,
            TimerType = TimerType.NONE
        };

        var result = await controller.Create(model);

        // Заявка должна быть отклонена, возвращается View с ошибками валидации
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    // ТЕСТ 3: Одобрение заявки администратором
    // Проверяет ключевую административную функцию - процесс одобрения заявки на участие
    // После одобрения статус заявки должен измениться с PENDING на APPROVED
    [Fact]
    public async Task ApplicationsAdminController_Approve_ApprovesApplication_WhenValid()
    {
        var context = CreateDbContext();
        var participant = new Participant
        {
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "User",
            Role = Role.PARTICIPANT,
            PasswordHash = "hash"
        };
        context.Participants.Add(participant);

        var admin = new Administrator
        {
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User",
            Role = Role.ADMINISTRATOR,
            PasswordHash = "hash"
        };
        context.Administrators.Add(admin);

        var car = new Car
        {
            ParticipantId = participant.Id,
            Brand = "Toyota",
            Model = "Corolla",
            CarClass = "A",
            LicensePlate = "ABC123"
        };
        context.Cars.Add(car);

        var eventItem = new Event
        {
            Title = "Test Event",
            Date = DateTime.Now.AddDays(1),
            Location = "Test Location",
            AdministratorId = admin.Id,
            Status = EventStatus.REGISTRATION_OPEN,
            MaxParticipants = 100,
            CarTypeRequirement = CarTypeRequirement.ANY
        };
        context.Events.Add(eventItem);

        // Создание заявки со статусом "Ожидает рассмотрения"
        var application = new Application
        {
            EventId = eventItem.Id,
            CarId = car.Id,
            ParticipantId = participant.Id,
            Status = ApplicationStatus.PENDING,
            ApplicationDate = DateTime.Now
        };
        context.Applications.Add(application);

        await context.SaveChangesAsync();

        var controller = CreateApplicationsAdminController(context, admin.Id);
        var result = await controller.Approve(application.Id);

        // После одобрения должен быть редирект на страницу списка заявок
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);

        // Проверка, что статус заявки изменился на "Одобрена"
        var updatedApplication = await context.Applications.FindAsync(application.Id);
        Assert.NotNull(updatedApplication);
        Assert.Equal(ApplicationStatus.APPROVED, updatedApplication.Status);
    }

    // ТЕСТ 4: Расчет результатов события
    // Проверяет сложную бизнес-логику расчета финальных результатов на основе времени кругов
    // Включает расчет позиций, сравнение времени и правильное определение победителя
    [Fact]
    public async Task LapTimesController_CalculateResults_CalculatesResults_WhenLapTimesExist()
    {
        var context = CreateDbContext();
        
        // Создание двух участников для сравнения результатов
        var participant1 = new Participant
        {
            Email = "test1@test.com",
            FirstName = "Test1",
            LastName = "User",
            Role = Role.PARTICIPANT,
            PasswordHash = "hash"
        };
        var participant2 = new Participant
        {
            Email = "test2@test.com",
            FirstName = "Test2",
            LastName = "User",
            Role = Role.PARTICIPANT,
            PasswordHash = "hash"
        };
        context.Participants.AddRange(participant1, participant2);

        var admin = new Administrator
        {
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User",
            Role = Role.ADMINISTRATOR,
            PasswordHash = "hash"
        };
        context.Administrators.Add(admin);

        var car1 = new Car
        {
            ParticipantId = participant1.Id,
            Brand = "Toyota",
            Model = "Corolla",
            CarClass = "A",
            LicensePlate = "ABC123"
        };
        var car2 = new Car
        {
            ParticipantId = participant2.Id,
            Brand = "Honda",
            Model = "Civic",
            CarClass = "A",
            LicensePlate = "XYZ789"
        };
        context.Cars.AddRange(car1, car2);

        var eventItem = new Event
        {
            Title = "Test Event",
            Date = DateTime.Now,
            Location = "Test Location",
            AdministratorId = admin.Id,
            Status = EventStatus.IN_PROGRESS,
            MaxParticipants = 100
        };
        context.Events.Add(eventItem);

        var application1 = new Application
        {
            EventId = eventItem.Id,
            CarId = car1.Id,
            ParticipantId = participant1.Id,
            Status = ApplicationStatus.APPROVED,
            ApplicationDate = DateTime.Now
        };
        var application2 = new Application
        {
            EventId = eventItem.Id,
            CarId = car2.Id,
            ParticipantId = participant2.Id,
            Status = ApplicationStatus.APPROVED,
            ApplicationDate = DateTime.Now
        };
        context.Applications.AddRange(application1, application2);

        // Запись времени кругов: участник 2 быстрее (1:15) чем участник 1 (1:20)
        var lapTime1 = new LapTime
        {
            ApplicationId = application1.Id,
            LapNumber = 1,
            Time = new TimeSpan(0, 1, 20, 0, 0),
            RecordedAt = DateTime.Now
        };
        var lapTime2 = new LapTime
        {
            ApplicationId = application2.Id,
            LapNumber = 1,
            Time = new TimeSpan(0, 1, 15, 0, 0),
            RecordedAt = DateTime.Now
        };
        context.LapTimes.AddRange(lapTime1, lapTime2);

        await context.SaveChangesAsync();

        var controller = CreateLapTimesController(context, admin.Id);
        var result = await controller.CalculateResults(eventItem.Id);

        // После расчета должен быть редирект
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);

        // Проверка, что результаты рассчитаны для обоих участников
        var finalResults = await context.FinalResults.ToListAsync();
        Assert.Equal(2, finalResults.Count);

        var result1 = finalResults.First(r => r.ApplicationId == application1.Id);
        var result2 = finalResults.First(r => r.ApplicationId == application2.Id);

        // Участник 2 должен быть на 1 месте (быстрее), участник 1 на 2 месте
        Assert.Equal(2, result1.Position);
        Assert.Equal(1, result2.Position);
        // Проверка, что время участника 2 действительно меньше
        Assert.True(result2.TotalTime < result1.TotalTime);
    }
}

// Мок-класс для имитации HTTP-сессии в тестах
// Позволяет тестировать контроллеры без реального HTTP-контекста
public class MockHttpSession : ISession
{
    private readonly Dictionary<string, byte[]> _sessionStorage = new Dictionary<string, byte[]>();

    public string Id => Guid.NewGuid().ToString();
    public bool IsAvailable => true;
    public IEnumerable<string> Keys => _sessionStorage.Keys;

    public void Clear()
    {
        _sessionStorage.Clear();
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task LoadAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        _sessionStorage.Remove(key);
    }

    public void Set(string key, byte[] value)
    {
        _sessionStorage[key] = value;
    }

    public bool TryGetValue(string key, out byte[] value)
    {
        if (_sessionStorage.TryGetValue(key, out var val))
        {
            value = val;
            return true;
        }
        value = null!;
        return false;
    }

    public void SetInt32(string key, int value)
    {
        var bytes = BitConverter.GetBytes(value);
        Set(key, bytes);
    }

    public void SetString(string key, string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        Set(key, bytes);
    }
}

// Мок-класс для имитации TempData в тестах
// TempData используется для передачи сообщений между запросами
public class MockTempDataProvider : ITempDataProvider
{
    private readonly Dictionary<string, object> _tempData = new Dictionary<string, object>();

    public IDictionary<string, object> LoadTempData(HttpContext context)
    {
        return _tempData;
    }

    public void SaveTempData(HttpContext context, IDictionary<string, object> values)
    {
        if (values == null)
        {
            _tempData.Clear();
            return;
        }

        _tempData.Clear();
        foreach (var kvp in values)
        {
            _tempData[kvp.Key] = kvp.Value;
        }
    }
}
