using ZenDriver.API.Security.Authorization.Handlers.Implementations;
using ZenDriver.API.Security.Authorization.Handlers.Interfaces;
using ZenDriver.API.Security.Authorization.Middleware;
using ZenDriver.API.Security.Authorization.Settings;
using ZenDriver.API.Security.Domain.Repositories;
using ZenDriver.API.Security.Domain.Services;
using ZenDriver.API.Security.Persistence.Repositories;
using ZenDriver.API.Security.Services;
using ZenDriver.API.Shared.Domain.Repositories;
using ZenDriver.API.Shared.Persistence.Contexts;
using ZenDriver.API.Shared.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);

// Add CORS
builder.Services.AddCors();

//Add services to the container
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

//AppSettings Configuration
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

builder.Services.AddSwaggerGen(options =>
{
    //Add API Documentation Information

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "INNOVAMIND Profile+ Center API",
        Description = "INNOVAMIND Profile+ Center API RESTful API",
        TermsOfService = new Uri("https://innova-mind.com/tos"),
        Contact = new OpenApiContact
        {
            Name = "INNOVAMIND.studio",
            Url = new Uri ("https://acme.studio")
        },
        License = new OpenApiLicense
        {
            Name = "INNOVAMIND Learning Center Resources License",
            Url = new Uri("https://innova-mind.com/license")
        }
    });
    options.EnableAnnotations();
    options.AddSecurityDefinition("bearearAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer Scheme"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearearAuth"}
            },
            Array.Empty<string>()
        }
    });
});



// Add Database Connection

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(
    options => options.UseMySQL(connectionString)
    .LogTo(Console.WriteLine, LogLevel.Information)
    .EnableSensitiveDataLogging()
    .EnableDetailedErrors());

//Add lowecase routes

builder.Services.AddRouting(options => options.LowercaseUrls = true);

// Dependency Injection Configuration

//Shared Injection Configuration

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();



// Security Injection Configuration

builder.Services.AddScoped<IJwtHandler, JwtHandler>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

//AutoMapper Configuration

builder.Services.AddAutoMapper(
    typeof(ZenDriver.API.Security.Mapping.ModelToResourceProfile),
    typeof(ZenDriver.API.Security.Mapping.ResourceToModelProfile)
    );

var app = builder.Build();

// Validation for ensuring Database Objects are created
using (var scope = app.Services.CreateScope())
using (var context = scope.ServiceProvider.GetService<AppDbContext>())
{
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("v1/swagger.json", "v1");
        options.RoutePrefix = "swagger";
        //Added To View the tags in swagger
        options.DisplayOperationId();
    });
}

// Configure CORS
app.UseCors(x => x
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
);

//Configure Error Handler Middleware
app.UseMiddleware<ErrorHandlerMiddleware>();

//Configure JWT Handling
app.UseMiddleware<JwtMiddleware>();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
