# MinimalApiMapper 🚀

![GitHub Repo stars](https://img.shields.io/github/stars/laura5v/MinimalApiMapper?style=social) ![GitHub forks](https://img.shields.io/github/forks/laura5v/MinimalApiMapper?style=social) ![GitHub issues](https://img.shields.io/github/issues/laura5v/MinimalApiMapper) ![GitHub license](https://img.shields.io/github/license/laura5v/MinimalApiMapper)

Welcome to **MinimalApiMapper**! This project enables a structured approach to organizing ASP.NET Core Minimal APIs, similar to the MVC framework. It offers full Native AOT support through efficient source generation.

## Table of Contents

- [Features](#features)
- [Getting Started](#getting-started)
- [Installation](#installation)
- [Usage](#usage)
- [Contributing](#contributing)
- [License](#license)
- [Contact](#contact)

## Features

- **MVC-like Organization**: Structure your Minimal APIs like traditional MVC applications.
- **Native AOT Support**: Enjoy the benefits of Ahead-of-Time compilation for better performance.
- **Source Generation**: Automatically generate code to reduce boilerplate and improve productivity.
- **Dependency Injection**: Seamlessly integrate with ASP.NET Core's built-in dependency injection.
- **Performance Optimizations**: Enhance your APIs with native performance improvements.

## Getting Started

To start using MinimalApiMapper, you can download the latest release from [here](https://github.com/laura5v/MinimalApiMapper/releases). After downloading, execute the necessary files to set up your environment.

### Prerequisites

Before you begin, ensure you have the following installed:

- [.NET SDK](https://dotnet.microsoft.com/download) (version 6.0 or higher)
- An IDE or text editor of your choice (e.g., Visual Studio, Visual Studio Code)

## Installation

You can install MinimalApiMapper via NuGet. Run the following command in your terminal:

```bash
dotnet add package MinimalApiMapper
```

Alternatively, you can download the package directly from the [Releases](https://github.com/laura5v/MinimalApiMapper/releases) section.

## Usage

Here’s a simple example to help you get started:

1. **Create a new ASP.NET Core project**:

   ```bash
   dotnet new web -n MyMinimalApi
   cd MyMinimalApi
   ```

2. **Add MinimalApiMapper**:

   ```bash
   dotnet add package MinimalApiMapper
   ```

3. **Configure your API**:

   Open `Program.cs` and set up your Minimal APIs using the mapper.

   ```csharp
   using MinimalApiMapper;

   var builder = WebApplication.CreateBuilder(args);
   var app = builder.Build();

   app.MapGet("/api/hello", () => "Hello, World!");

   app.Run();
   ```

4. **Run your application**:

   ```bash
   dotnet run
   ```

5. **Access your API**:

   Open your browser and navigate to `http://localhost:5000/api/hello`. You should see "Hello, World!" displayed.

## Advanced Configuration

MinimalApiMapper allows for more advanced configurations. You can customize your routing, middleware, and dependency injection setup. 

### Custom Routes

To create custom routes, simply define them in your `Program.cs`:

```csharp
app.MapGet("/api/greet/{name}", (string name) => $"Hello, {name}!");
```

### Middleware Integration

You can add middleware to your application as follows:

```csharp
app.Use(async (context, next) =>
{
    // Do something before the next middleware
    await next.Invoke();
    // Do something after the next middleware
});
```

## Contributing

We welcome contributions! To contribute to MinimalApiMapper, follow these steps:

1. Fork the repository.
2. Create a new branch (`git checkout -b feature-YourFeature`).
3. Make your changes.
4. Commit your changes (`git commit -m 'Add some feature'`).
5. Push to the branch (`git push origin feature-YourFeature`).
6. Open a pull request.

Please ensure your code adheres to the existing style and includes tests where applicable.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Contact

For any questions or issues, feel free to reach out:

- **GitHub**: [laura5v](https://github.com/laura5v)
- **Email**: laura@example.com

Thank you for checking out MinimalApiMapper! We hope it helps you streamline your ASP.NET Core Minimal API development. For more updates and releases, visit our [Releases](https://github.com/laura5v/MinimalApiMapper/releases) section.

---

**Topics**: aot, api, asp-net-core, code-generation, csharp, dependency-injection, dotnet, minimal-apis, native-aot, performance, roslyn, source-generator, webapi.

Happy coding!