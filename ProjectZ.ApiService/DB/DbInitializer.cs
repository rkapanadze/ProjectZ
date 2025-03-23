using Dapper;
using Npgsql;

namespace ProjectZ.ApiService.DB;

internal sealed class DbInitializer(
    NpgsqlDataSource dataSource,
    IConfiguration configuration,
    ILogger<DbInitializer> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Initializing database ...");
            await EnsureDatabaseExists();
            await InitializeDatabase(); // Ensure schema creation
            await SeedInitialData();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    private async Task EnsureDatabaseExists()
    {
        string connectionString = configuration.GetConnectionString("projectzdb");
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        string? databaseName = builder.Database;
        builder.Database = "postgres"; // Connect to the default "postgres" database

        using var connection = new NpgsqlConnection(builder.ToString());
        await connection.OpenAsync();

        // Check if the database exists
        bool dbExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM pg_database WHERE datname = @databaseName)", new
            {
                databaseName
            });

        if (!dbExists)
        {
            logger.LogInformation("Creating Database...{databaseName}", databaseName);
            await connection.ExecuteAsync($"CREATE DATABASE {databaseName}");
            logger.LogInformation("Database created successfully.");
        }
        else
        {
            logger.LogInformation("Database already exists.");
        }

        await connection.CloseAsync();
    }

    private async Task InitializeDatabase()
    {
        string connectionString = configuration.GetConnectionString("projectzdb");
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        using var connection = new NpgsqlConnection(builder.ToString());
        await connection.OpenAsync();
        logger.LogInformation("Database tables created or verified.");
        // Create tables
        var createTableSql = @"
                CREATE TABLE IF NOT EXISTS public.Users (
                    Id SERIAL PRIMARY KEY,
                    Username VARCHAR(50) NOT NULL,
                    Email VARCHAR(100) NOT NULL,
                    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT unique_username UNIQUE (Username)
                );

                CREATE TABLE IF NOT EXISTS public.Products (
                    Id SERIAL PRIMARY KEY,
                    Name VARCHAR(100) NOT NULL,
                    Price DECIMAL(10, 2) NOT NULL,
                    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT unique_product_name UNIQUE (Name)
                );
            ";

        await connection.ExecuteAsync(createTableSql);
        logger.LogInformation("Database tables created or verified.");
        await connection.CloseAsync();
    }

    private async Task SeedInitialData()
    {
        string connectionString = configuration.GetConnectionString("projectzdb");
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        using var connection = new NpgsqlConnection(builder.ToString());
        await connection.OpenAsync();

        // Seed data into Users table
        var userSql = @"
            INSERT INTO public.Users (Username, Email)
            VALUES
            ('john_doe', 'john.doe@example.com'),
            ('jane_smith', 'jane.smith@example.com')
            ON CONFLICT (Username) DO NOTHING;
        ";

        // Seed data into Products table
        var productSql = @"
            INSERT INTO public.Products (Name, Price)
            VALUES
            ('Product 1', 19.99),
            ('Product 2', 29.99),
            ('Product 3', 39.99)
            ON CONFLICT (Name) DO NOTHING;
        ";

        await connection.ExecuteAsync(userSql);
        await connection.ExecuteAsync(productSql);

        logger.LogInformation("Initial data seeded into the database.");
        await connection.CloseAsync();
    }
}