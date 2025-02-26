using System.Text;
using BankBackend.Data;
using BankBackend.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);


//add controller
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "provide token",
        Name = "Authprization",
        Type = SecuritySchemeType.Http,
        BearerFormat="JWT",
        Scheme="bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{ }
        }
    });
});



// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:4000", "http://localhost:8080") // Allow React frontend
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

/*add the dbcontext to sdipendency injection container*/
builder.Services.AddDbContext<BankContex>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BankBackendContext")));

// Add Identity services, specifying BankUser as the user model,
// tie it to our BankContex so Identity can store user data in the same database
builder.Services.AddIdentityApiEndpoints<BankUser>()
        .AddEntityFrameworkStores<BankContex>();


//Add JWT-based authentication.
builder.Services.AddAuthentication(options =>
{
    // Set Bearer as the default authentication scheme
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

}).AddJwtBearer(options =>
{
    // Read the secret key from configuration
    var secretKey = builder.Configuration["JwtSettings:SecretKey"];
    // Convert to a SymmetricSecurityKey for signing JWT
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = key,
        ValidateIssuer = false,  
        ValidateAudience = false, 
        ValidateLifetime = true   // ensure token hasn't expired
    };
});




builder.Services.AddAuthorization();
//logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();
//login
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application is starting...");


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();
//add cors middleweart
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();
//Routes HTTP requests to the controller actions
app.MapControllers();

app.Run();
