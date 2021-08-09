using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using Xunit;

namespace Hipster.Api.Tests
{
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
}