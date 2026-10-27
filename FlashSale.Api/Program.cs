var builder = WebApplication.CreateBuilder(args);

// OrderService servisimizi kaydediyoruz
builder.Services.AddScoped<FlashSale.Api.Services.OrderService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger Türkçe Başlık ve XML Yorum Desteği
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Flash Satış API Servisi",
        Version = "v1",
        Description = "Yüksek eşzamanlılık ve stok güvenliği sağlayan indirimli bilet/ürün satış altyapısı."
    });

    // Controller üzerındaki XML yorumlarını Swagger'a aktarma
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Flash Satış API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();