### Intro

- Explain what we're doing
  - Building a book store app
  - Not focusing on business logic
  - Not focusing too much on how ASP.NET Core or React works
  - Not focusing on code quality, aiming for simplest and dumbest solution
  - Focusing on infrastructure

- Outline installed tools and SDKs
  - VS Code
  - Dotnet SDK
  - Node SDK
  - Docker Desktop
  - pgAdmin4
- Outline VS Code extensions

### Bootstrapping the backend

- `dotnet new sln`
- `dotnet new gitignore`
- `dotnet new web -o Hipster.Api`  
- `dotnet sln add Hipster.Api`
- `dotnet watch run`
- `dotnet add package NSwag.AspNetCore`
- Configure OpenAPI and routing:

```csharp
// Startup.cs

services.AddControllers();
services.AddOpenApiDocument(options =>
{
    options.Title = "Hipster.Api";
});

// ...
app.UseOpenApi();
app.UseSwaggerUi3();
app.UseEndpoints(endpoints => endpoints.MapControllers());
```

- Add `Book` model:

```csharp
// Book.cs

public class Book
{
    [Required]
    public int Id { get; init; }

    [Required]
    public string Title { get; init; }

    [Required]
    public string Author { get; init; }

    [Required]
    public string ISBN { get; init; }

    [Required]
    public int Year { get; init; }

    [Required]
    public string CoverImageUrl { get; init; }
}
```

- Add the controller:

```csharp
// BookController.cs

[Route("[controller]")]
[ApiController]
public class BooksController : Controller
{
    private readonly DatabaseContext _db;

    public BooksController(DatabaseContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(Book[]), 200)]
    public async Task<IActionResult> GetBooks()
    {
        var books = await _db.Books.ToArrayAsync();
        return Ok(books);
    }
}
```

- Add OpenAPI annotations:

```csharp
// BookController.cs

[ProducesResponseType(typeof(Book[]), 200)]
// ...
```

- Load the OpenAPI spec into https://editor.swagger.io

### Adding a persistence layer

- `docker run --rm -it -e POSTGRES_PASSWORD=mysecretpassword -p 5432:5432 postgres`
- `dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL`
- Create a class derived from `DbContext` and add it to the service container:

```csharp
services.AddDbContext<DatabaseContext>(options =>
    options.UseNpgsql(Configuration.GetConnectionString("Database"))
);
```

- Configure connection string: `Host=localhost;Username=postgres;Password=mysecretpassword`
- Seed data through `DatabaseContext` (add at least 5 books):

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Book>().HasData(
        new Book
        {
            Id = 1,
            Title = "The Kite Runner",
            Author = "Khaled Hosseini",
            ISBN = "978-1-93778-812-0",
            Year = 2014,
            ImageUrl = "https://images-na.ssl-images-amazon.com/images/I/51tXQ%2B%2B%2B._SX300_BO1,204,203,200_.jpg"
        }
    );
}
```

- Run the API and connect to the database with pgAdmin

### Bootstrapping the frontend

- `npx create-react-app hipster.app_rename --template typescript`
  - Rename the folder to `Hipster.App`
  - Remove `.git` folder from `Hipster.App`
- Explain the files added by CRA
- Hardcode API url in `config.ts`
- `useState(...)` and `useEffect(...)` to pull data from the API
- Try to test it out and notice that CORS is blocking the requests
- Configure CORS on backend:

```csharp
// Startup.cs

services.AddCors();

// ...

app.UseCors(o =>
{
    o.WithOrigins("*");
    o.AllowAnyHeader();
    o.AllowAnyMethod();
});
```

### Generating OpenAPI client

- `npm i nswag`
- Add configuration file for NSwag

```json
// nswag.json

{
  "runtime": "Net50",
  "documentGenerator": {
    "aspNetCoreToOpenApi": {
      "project": "../Hipster.Api/Hipster.Api.csproj",
      "outputType": "Swagger2",
      "noBuild": true,
      "newLineBehavior": "Auto"
    }
  },
  "codeGenerators": {
    "openApiToTypeScriptClient": {
      "typeScriptVersion": 2.7,
      "template": "Fetch",
      "dateTimeType": "String",
      "nullValue": "Undefined",
      "generateClientClasses": true,
      "generateClientInterfaces": false,
      "generateOptionalParameters": true,
      "generateDefaultValues": true,
      // Where the output file will be saved
      "output": "./src/api.generated.ts"
    }
  }
}
```

- Add corresponding script to `package.json`:

```json
"generate-client": "nswag run /Runtime:Net50"
```

- Go through the generated client to explain what it does
- Add API module:

```typescript
// api.ts
import * as generated from "./api.generated";
import config from "./config";

const clients = {
    books: new generated.BooksClient(config.apiUrl)
}

export default clients;
```

- `npm i react-query`
- Add query client context provider:

```tsx
// index.tsx
const queryClient = new QueryClient();

ReactDOM.render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <App />
    </QueryClientProvider>
  </React.StrictMode>,
  document.getElementById("root")
);
```

- Use react-query to fetch books:

```tsx
// App.tsx

function BookList() {
  const booksQuery = useQuery("all-books", () => api.books.getBooks());

  if (booksQuery.isLoading) {
    return <div>Loading...</div>;
  }

  if (booksQuery.error) {
    return <div>Error: {(booksQuery.error as {}).toString()}</div>;
  }

  return (
    <div>
      {booksQuery.data!.map((book) => (
        <div key={book.id}>
          <h3>{book.title}</h3>
          <div>{book.author}</div>
          <div>{book.isbn}</div>
          <div>{book.year}</div>
        </div>
      ))}
    </div>
  );
}

function App() {
  // ...
}
```

- Add `[Required]` to `Book.cs` and regenerate client to make fields non-nullable

### Dockerizing the backend

- Add dockerfile for API:

```dockerfile
# api.dockerfile

# ** Build

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src

COPY Hipster.Api Hipster.Api

# Ideally should run restore before build, but it's not critical
RUN dotnet publish Hipster.Api -o Hipster.Api/artifacts -c Release

# ** Run

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS run
WORKDIR /app

EXPOSE 80
EXPOSE 443

COPY --from=build /src/Hipster.Api/artifacts ./

ENTRYPOINT ["dotnet", "Hipster.Api.dll"]
```

- Make note that it's better to use self-contained publish model for Docker images
  - https://twitter.com/runfaster2000/status/1420430959803965441?s=20
  - https://github.com/dotnet/dotnet-docker/blob/b79dff9e3d27fee509ed871f2ca3d8b0221e05e6/samples/aspnetapp/Dockerfile.ubuntu-x64-slim
  - https://github.com/dotnet/designs/blob/ec082c3aae31c918db05a043a8e1a6c38b08b913/accepted/2021/architecture-targeting.md#desired-build-types
- `docker build -t hipster.api . -f api.dockerfile`
- Create network: `docker network create hipsternet`
- Run DB: `docker run --rm -it -e POSTGRES_PASSWORD=mysecretpassword -p 5432:5432 --net hipsternet --name hipster.db postgres`
- Run API: `docker run --rm -it -e ConnectionStrings__Database="Host=hipster.db;Username=postgres;Password=mysecretpassword" -p 80:80 --net hipsternet hipster.api`
- Add compose config:

```yml
# docker-compose.yml

version: "3.7"

services:
  hipster.db:
    image: postgres
    environment:
      POSTGRES_PASSWORD: "mysecretpassword"
    ports:
      - "5432:5432"

  hipster.api:
    build:
      context: .
      dockerfile: "api.dockerfile"
    environment:
      ConnectionStrings__Database: "Host=hipster.db;Username=postgres;Password=mysecretpassword"
    ports:
      # Same port as when running locally, for consistency
      - "5000:80"
    depends_on:
      - "hipster.db"
```

- `docker compose up --build`
- `docker compose down` (optionally with `--rmi all`)

### Dockerizing the frontend

- Show what `npm build` does
- Make API URL configurable:

```tsx
const config = {
    apiUrl: process.env['REACT_APP_API_URL'] || 'http://localhost:5000',
    appUrl: process.env['PUBLIC_URL'] || 'http://localhost:3000'
}

export default config;
```

- `app.dockerfile`:

```dockerfile
# ** Build

FROM node:12 as build
WORKDIR /app

# React app can't access env vars after it's built
# so we need to set parameters before the build
ARG API_URL=http://localhost:5000
ARG APP_URL=http://localhost:3000

ENV REACT_APP_API_URL=${API_URL}
ENV PUBLIC_URL=${APP_URL}

COPY Hipster.App/package.json .
COPY Hipster.App/package-lock.json .

RUN npm install --silent

COPY Hipster.App/tsconfig.json .
COPY Hipster.App/public/ public/
COPY Hipster.App/src/ src/

RUN npm run build

# ** Run

FROM nginx:1.16.0 as run

EXPOSE 80
EXPOSE 443

COPY --from=build /app/build /usr/share/nginx/html

ENTRYPOINT ["nginx", "-g", "daemon off;"]
```

- `docker-compose.yml`:

```yml
  hipster.app:
    build:
      context: .
      dockerfile: "app.dockerfile"
      args:
        - API_URL=http://localhost:5000
        - APP_URL=http://localhost:3000
    ports:
      - "3000:80"
    depends_on:
      - "hipster.api"
```

### Making things slightly more interesting

- Add `/books/{id}` endpoint:

```csharp
// BooksController.cs

[HttpGet("{id}")]
[ProducesResponseType(typeof(Book), 200)]
[ProducesResponseType(typeof(ProblemDetails), 404)]
public async Task<IActionResult> GetBook(int id)
{
    var book = await _db.Books.FindAsync(id);

    if (book is null)
        return NotFound();

    return Ok(book);
}
```

- Regenerate client
- `npm i react-router-dom @types/react-router @types/react-router-dom`

```tsx
// App.tsx
import { Route, Switch } from "react-router";
import { BrowserRouter } from "react-router-dom";
import BookPage from "./BookPage";
import BookListPage from "./BookListPage";

function PageRouter() {
  return (
    <BrowserRouter>
      <Switch>
        <Route exact path="/:bookId" component={BookPage} />
        <Route exact path="/" component={BookListPage} />
      </Switch>
    </BrowserRouter>
  );
}

export default function App() {
  return (
    <main>
      <PageRouter />
    </main>
  );
}

// BookListPage.tsx
import api from "./api";
import { Link } from "react-router-dom";
import { useQuery } from "react-query";

export default function BookListPage() {
  const booksQuery = useQuery("all-books", () => api.books.getBooks(), {
    retry: false,
  });

  if (booksQuery.isLoading) {
    return <div>Loading...</div>;
  }

  if (booksQuery.error) {
    return <div>Error: {(booksQuery.error as {}).toString()}</div>;
  }

  return (
    <div>
      {booksQuery.data!.map((book) => (
        <div key={book.id}>
          <h3>
            <Link to={`/${book.id}`}>{book.title}</Link>
          </h3>
          <div>{book.author}</div>
          <div>{book.isbn}</div>
          <div>{book.year}</div>
        </div>
      ))}
    </div>
  );
}

// BookPage.tsx
import api from "./api";
import { useParams } from "react-router";
import { useQuery } from "react-query";

export default function BookPage() {
  const { bookId } = useParams<{ bookId?: string }>();

  const bookQuery = useQuery(
    `book-${bookId}`,
    () => api.books.getBook(Number(bookId)),
    {
      retry: false,
    }
  );

  if (bookQuery.isLoading) {
    return <div>Loading...</div>;
  }

  if (bookQuery.error) {
    return <div>Error: {(bookQuery.error as {}).toString()}</div>;
  }

  return (
    <div>
      <div>
        <h3>{bookQuery.data!.title}</h3>
        <div>{bookQuery.data!.author}</div>
        <div>{bookQuery.data!.isbn}</div>
        <div>{bookQuery.data!.year}</div>
      </div>
    </div>
  );
}
```

- Show error when requesting non-existing book and realize it doesn't properly show (see console output)
- Add an error handler:

```tsx
// shared/Error.tsx
function resolveError(error: unknown): string | null {
  if (!error) {
    return null;
  }

  if (typeof error === "string") {
    return error;
  }

  if (typeof error === "object") {
    const errorRecord = error as Record<string, unknown>;

    if (errorRecord.message && typeof errorRecord.message === "string") {
      return errorRecord.message;
    }

    if (errorRecord.detail && typeof errorRecord.detail === "string") {
      return errorRecord.detail;
    }

    if (errorRecord.title && typeof errorRecord.title === "string") {
      return errorRecord.title;
    }

    return errorRecord.toString();
  }

  return null;
}

interface ErrorProps {
  error: unknown;
}

export default function Error({ error }: ErrorProps) {
  const resolvedError = resolveError(error);
  if (!resolvedError) {
    return <></>;
  }

  return <span>{resolvedError}</span>;
}
```

- Show that fetch errors are also handled
- `npm i @chakra-ui/react @emotion/react@^11 @emotion/styled@^11 framer-motion@^4`
- Revamp the pages:

```tsx
// index.tsx
ReactDOM.render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <ChakraProvider>
        <App />
      </ChakraProvider>
    </QueryClientProvider>
  </React.StrictMode>,
  document.getElementById("root")
);
```

```tsx
// App.tsx
export default function App() {
  return (
    <Box>
      <Center as="header" padding={5} bg="beige">
        <Heading>Hipster Books</Heading>
      </Center>

      <Box as="main" padding={5} width="50%" margin="auto">
        <PageRouter />
      </Box>
    </Box>
  );
}
```

```tsx
// BookListPage.tsx
return (
  <Box>
    <Breadcrumb>
      <BreadcrumbItem>
        <BreadcrumbLink as={RouterLink} to="/" color="blue.500">
          Home
        </BreadcrumbLink>
      </BreadcrumbItem>
    </Breadcrumb>

    <Box fontSize="3xl" marginTop="1em">
      Books
    </Box>

    <Wrap spacing="2em">
      {booksQuery.data!.map((book) => (
        <Box key={book.id}>
          <Image src={book.coverImageUrl} width="100px" />
          <Link
            as={RouterLink}
            to={`/${book.id}`}
            fontSize="xl"
            fontWeight="semibold"
            color="blue.500"
          >
            {book.title}
          </Link>
          <Box fontWeight="semibold">{book.author}</Box>
          <Box>ISBN: {book.isbn}</Box>
          <Box>Published: {book.year}</Box>
        </Box>
      ))}
    </Wrap>
  </Box>
);
```

```tsx
// BookPage.tsx
return (
  <Box>
    <Breadcrumb>
      <BreadcrumbItem>
        <BreadcrumbLink as={RouterLink} to="/" color="blue.500">
          Home
        </BreadcrumbLink>
      </BreadcrumbItem>

      <BreadcrumbItem>
        <BreadcrumbLink
          as={RouterLink}
          to={`/${bookQuery.data!.id}`}
          color="blue.500"
        >
          {bookQuery.data!.title}
        </BreadcrumbLink>
      </BreadcrumbItem>
    </Breadcrumb>

    <Box fontSize="3xl" marginTop="1em">
      {bookQuery.data!.title}
    </Box>

    <Wrap spacing="2em">
      <Box key={bookQuery.data!.id}>
        <Image src={bookQuery.data!.coverImageUrl} width="300px" />
        <Box fontWeight="semibold">{bookQuery.data!.author}</Box>
        <Box>ISBN: {bookQuery.data!.isbn}</Box>
        <Box>Published: {bookQuery.data!.year}</Box>
      </Box>
    </Wrap>
  </Box>
);
```

### Writing tests for backend

- `dotnet new xunit -o Hipster.Api.Tests`
- `dotnet sln add Hipster.Api.Tests`
- Configure xUnit:

```json
// xunit.runner.json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "methodDisplayOptions": "all",
  "methodDisplay": "method"
}
```

- `dotnet add package Microsoft.AspNetCore.Mvc.Testing`
- `dotnet add package CliWrap`
- `dotnet add package FluentAssertions`
- `dotnet add package JsonExtensions`
- Mention test containers: https://github.com/HofmeisterAn/dotnet-testcontainers
- Add Postgres fixture:

```csharp
// PostgresFixture.cs

public class PostgresFixture : IAsyncLifetime
{
    private string _postgresContainerId;

    public async Task InitializeAsync()
    {
        var runResult = await Cli.Wrap("docker")
            .WithArguments(new[]
            {
                "run",
                "--rm",
                "-d",
                "-e", "POSTGRES_PASSWORD=mysecretpassword",
                "-p", "5432:5432",
                "postgres"
            })
            .ExecuteBufferedAsync();

        _postgresContainerId = runResult.StandardOutput.Trim();

        // Wait until Postgres is ready to accept connections
        while (true)
        {
            var probleResult = await Cli.Wrap("docker")
                .WithArguments(new[]
                {
                    "exec",
                    _postgresContainerId,
                    "pg_isready"
                })
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (probleResult.ExitCode == 0)
                break;

            await Task.Delay(100);
        }
    }

    public async Task DisposeAsync()
    {
        await Cli.Wrap("docker")
            .WithArguments(new[] { "stop", _postgresContainerId })
            .ExecuteBufferedAsync();
    }
}
```

- Add a test for `/` endpoint:

```csharp
// BookSpecs.cs

[Fact]
public async Task GET_request_to_books_endpoint_returns_all_books()
{
    // Arrange
    using var appFactory = new WebApplicationFactory<Startup>();
    using var client = appFactory.CreateClient();

    // Act & assert
    using var response = await client.GetAsync("/books");
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var json = await response.Content.ReadAsJsonAsync();

    var books = json.EnumerateArray().ToArray();
    books.Should().HaveCount(2);

    var book1 = books[0];
    book1.GetProperty("title").GetString().Should().Be("The Kite Runner");
    book1.GetProperty("isbn").GetString().Should().Be("978-1-93778-812-0");

    var book2 = books[1];
    book2.GetProperty("title").GetString().Should().Be("The Time Machine");
    book2.GetProperty("isbn").GetString().Should().Be("978-1-93778-812-0");
}
```

- Add tests for `/{bookId}` endpoint:

```csharp
[Fact]
public async Task GET_request_to_books_endpoint_with_book_ID_returns_a_specific_book()
{
    // Arrange
    using var appFactory = new WebApplicationFactory<Startup>();
    using var client = appFactory.CreateClient();

    // Act & assert
    using var response = await client.GetAsync("/books/1");
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var json = await response.Content.ReadAsJsonAsync();

    json.GetProperty("title").GetString().Should().Be("The Kite Runner");
    json.GetProperty("isbn").GetString().Should().Be("978-1-93778-812-0");
}

[Fact]
public async Task GET_request_to_books_endpoint_with_book_ID_returns_an_error_for_non_existing_book()
{
    // Arrange
    using var appFactory = new WebApplicationFactory<Startup>();
    using var client = appFactory.CreateClient();

    // Act & assert
    using var response = await client.GetAsync("/books/99999");
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
```

- Show how the container starts and gets killed as the test runs (in Docker Desktop GUI)

- Add CI workflow through GitHub Actions:

```yml
# .github/workflows/CI.yml

name: CI

on: [push, pull_request]

jobs:
  build-api:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v2.3.3

      - name: Install .NET
        uses: actions/setup-dotnet@v1.7.2
        with:
          dotnet-version: 5.0.x

      - name: Build & test
        run: dotnet test --configuration Release

  build-app:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v2.3.3

      - name: Install NodeJS
        uses: actions/setup-node@v2.1.2
        with:
          node-version: 12

      - name: Pull dependencies
        run: npm ci --no-audit
        working-directory: ./Hipster.App

      - name: Build
        run: npm run build
        working-directory: ./Hipster.App
```

- Add coverage reporting to CI
- Fail a test and see that it reports poorly
- `dotnet add package GitHubActionsTestLogger`
- Configure the test logger:

```yml
# .github/workflows/CI.yml

# ...

- name: Build & test
  run: dotnet test --configuration Release --logger GitHubActions
```

- Build Docker container in CI:

```yml
# .github/workflows/CI.yml

# ...

build-docker:
  runs-on: ubuntu-latest
  steps:
    - name: Checkout
      uses: actions/checkout@v2.3.3

    - name: Build containers
      run: docker-compose build
```

### Writing tests for frontend

- Out of scope
- Read more: https://testing-library.com/docs/react-testing-library/example-intro

### Deploying backend

- Talk about Heroku and its pricing tiers
- Add a new app, connect it to GitHub repository, and enable automatic deploys
- Attempt to deploy the app, see that it fails because it doesn't know how to build it
- Configure build pack `jincod/dotnetcore`
- Deploy the app and open the swagger dashboard
- Note that the database connection is not working
- Add Heroku Postgres resource and explain how the connection string is exposed
- Add handling for Heroku Postgres URLs:

```csharp
// HerokuIntegration.cs

internal static class HerokuIntegration
{
    private static string ConnectionStringFromUrl(string url)
    {
        // postgres://user:password@host:port/database

        var uri = new Uri(url);
        var builder = new NpgsqlConnectionStringBuilder();

        var userParts = uri.UserInfo.Split(':');

        builder.Username = userParts[0];
        builder.Password = userParts[1];
        builder.Host = uri.Host;
        builder.Port = uri.Port;
        builder.Database = uri.AbsolutePath.TrimStart('/');
        builder.SslMode = SslMode.Prefer;
        builder.TrustServerCertificate = true;

        return builder.ConnectionString;
    }

    public static IWebHostBuilder UseHeroku(this IWebHostBuilder webHost)
    {
        // Port settings
        var port = Environment.GetEnvironmentVariable("PORT");
        if (!string.IsNullOrWhiteSpace(port))
        {
            webHost.UseUrls("http://*:" + port);
        }

        // Database settings
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__Database",
                ConnectionStringFromUrl(databaseUrl)
            );
        }

        return webHost;
    }
}
```

```csharp
// Program.cs

public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder
                .UseStartup<Startup>()
                .UseHeroku();
        });
```

- Try again, see that the app successfully connects to the database
- Connect to Heroku Postgres instance via pgAdmin4
- Connect to the app logs via Heroku CLI
  - https://devcenter.heroku.com/articles/heroku-cli#download-and-install
  - `heroku login`
  - `heroku logs -a hipster-app --tail`

### Deploying frontend

- Talk about Netlify and its pricing tiers
- Add a new site and connect it to GitHub repository
- Change the site name to `hipster-app`
- Configure base directory (`Hipster.App`), publish directory (`Hipster.App/build`), and command (`npm run build`)
- Attempt to deploy the site, see that it cannot connect to the backend
- Add environment variables:
  - `REACT_APP_API_URL`: `https://hipster-app.herokuapp.com`
  - `PUBLIC_URL`: `https://hipster-app.netlify.app`
- Deploy the site and see that it works
- Open a non-root page, refresh, and see that it doesn't work
- Configure rewrite rules:

```sh
# Hipster.App/public/_redirects

/* /index.html 200
```

- Make CORS configurable on the backend:

```json
// appsettings.Development.json

{
    "AllowedOrigins": "*",
    // ...
}
```

```csharp
// Startup.cs

// ...
app.UseCors(o =>
{
    o.WithOrigins(Configuration.GetSection("AllowedOrigins").Get<string>()?.Split(',') ?? new[] {"*"});
    o.AllowAnyHeader();
    o.AllowAnyMethod();
});
```

### Switch from automatic to manual deployments

- Disable automatic deploy on Heroku
- Add CD workflow for backend:

```yml
# cd.yml

name: CD

# Change to only run on tags in the future
on: [push]

jobs:
  deploy-api:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v2.3.3

      - name: Build
        run: docker build . -f api.dockerfile -t hipster.api

      - name: Install Heroku CLI
        run: curl https://cli-assets.heroku.com/install.sh | sh

      - name: Log in
        run: heroku container:login
        env:
          HEROKU_API_KEY: ${{ secrets.HEROKU_TOKEN }}

      - name: Deploy
        run: |
          docker tag hipster.api registry.heroku.com/${{ secrets.HEROKU_APP }}/web
          docker push registry.heroku.com/${{ secrets.HEROKU_APP }}/web

      - name: Release
        run: heroku container:release web -a ${{ secrets.HEROKU_APP }}
        env:
          HEROKU_API_KEY: ${{ secrets.HEROKU_TOKEN }}
```

- Disable automatic deploy on Netlify
  - Go to deploy settings, edit, check "Stop builds"
  - DO NOT disable autopublishing
- Add CD workflow for frontend:

```yml
# cd.yml

# ...

deploy-app:
  needs: deploy-api
  runs-on: ubuntu-latest
  steps:
    - name: Checkout
      uses: actions/checkout@v2.3.3

    - name: Install NodeJS
      uses: actions/setup-node@v2.1.2
      with:
        node-version: 12

    - name: Install Netlify CLI
      run: npm i netlify-cli -g

    - name: Pull dependencies
      run: npm ci --no-audit
      working-directory: ./Hipster.App

    - name: Build
      run: npm run build
      working-directory: ./Hipster.App
      env:
        NODE_ENV: production
        PUBLIC_URL: https://hipster-app.netlify.app
        REACT_APP_API_URL: https://hipster-app.herokuapp.com

    - name: Deploy
      run: netlify deploy --dir=build --prod
      working-directory: ./Hipster.App
      env:
        NETLIFY_SITE_ID: ${{ secrets.NETLIFY_SITE }}
        NETLIFY_AUTH_TOKEN: ${{ secrets.NETLIFY_TOKEN }}
```

### Adding services

- Add Papertrail
- Add Sentry
- `dotnet add package Sentry.AspNetCore`
- Configure Sentry:

```csharp
// Program.cs

// ...

public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder
                .UseStartup<Startup>()
                .UseHeroku()
                .UseSentry();
        });
```
