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
builder.Services.AddControllers();

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
        ValidateIssuer = false,   //  no issuer validation
        ValidateAudience = false, //  no audience validation
        ValidateLifetime = true   // ensure token hasn't expired
    };
});




builder.Services.AddAuthorization();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
//Routes HTTP requests to the controller actions
app.MapControllers();

app.Run();
