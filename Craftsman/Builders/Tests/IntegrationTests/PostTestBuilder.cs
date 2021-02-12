﻿namespace Craftsman.Builders.Tests.IntegrationTests
{
    using Craftsman.Enums;
    using Craftsman.Exceptions;
    using Craftsman.Helpers;
    using Craftsman.Models;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using static Helpers.ConsoleWriter;

    public class PostTestBuilder
    {
        public static void CreateEntityWriteTests(string solutionDirectory, Entity entity, string solutionName, bool addJwtAuth, List<Policy> policies)
        {
            try
            {
                var classPath = ClassPathHelper.TestEntityIntegrationClassPath(solutionDirectory, $"Create{entity.Name}IntegrationTests.cs", entity.Name, solutionName);

                if (!Directory.Exists(classPath.ClassDirectory))
                    Directory.CreateDirectory(classPath.ClassDirectory);

                if (File.Exists(classPath.FullClassPath))
                    throw new FileAlreadyExistsException(classPath.FullClassPath);

                using (FileStream fs = File.Create(classPath.FullClassPath))
                {
                    var data = CreateIntegrationTestFileText(classPath, entity, solutionDirectory, solutionName, addJwtAuth, policies);
                    fs.Write(Encoding.UTF8.GetBytes(data));
                }

                GlobalSingleton.AddCreatedFile(classPath.FullClassPath.Replace($"{solutionDirectory}{Path.DirectorySeparatorChar}", ""));
            }
            catch (FileAlreadyExistsException e)
            {
                WriteError(e.Message);
                throw;
            }
            catch (Exception e)
            {
                WriteError($"An unhandled exception occurred when running the API command.\nThe error details are: \n{e.Message}");
                throw;
            }
        }

        private static string CreateIntegrationTestFileText(ClassPath classPath, Entity entity, string solutionDirectory, string solutionName, bool addJwtAuth, List<Policy> policies)
        {
            var assertString = "";
            foreach (var prop in entity.Properties)
            {
                var newLine = prop == entity.Properties.LastOrDefault() ? "" : $"{Environment.NewLine}";
                assertString += @$"                {entity.Name.LowercaseFirstLetter()}ById.{prop.Name}.Should().Be(fake{entity.Name}.{prop.Name});{newLine}";
            }
            var httpClientExtensionsClassPath = ClassPathHelper.HttpClientExtensionsClassPath(solutionDirectory, solutionName, $"HttpClientExtensions.cs");
            var authUsing = addJwtAuth ? $@"
    using {httpClientExtensionsClassPath.ClassNamespace};" : "";

            var authOnlyTests = $@"
            {CreateEntityTestUnauthorized(entity)}
            {CreateEntityTestForbidden(entity)}";

            return @$"
namespace {classPath.ClassNamespace}
{{
    using Application.Dtos.{entity.Name};
    using FluentAssertions;
    using {solutionName}.Tests.Fakes.{entity.Name};
    using Microsoft.AspNetCore.Mvc.Testing;
    using System.Threading.Tasks;
    using Xunit;
    using Newtonsoft.Json;
    using System.Net.Http;
    using WebApi;
    using System.Collections.Generic;
    using Application.Wrappers;{authUsing}

    [Collection(""Sequential"")]
    public class {Path.GetFileNameWithoutExtension(classPath.FullClassPath)} : IClassFixture<CustomWebApplicationFactory>
    {{ 
        private readonly CustomWebApplicationFactory _factory;

        public {Path.GetFileNameWithoutExtension(classPath.FullClassPath)}(CustomWebApplicationFactory factory)
        {{
            _factory = factory;
        }}

        {CreateEntityTest(entity, addJwtAuth, policies)}{authOnlyTests}
    }} 
}}";
        }

        private static string CreateEntityTest(Entity entity, bool addJwtAuth, List<Policy> policies)
        {
            var assertString = "";
            foreach (var prop in entity.Properties.Where(p => p.IsPrimaryKey == false))
            {
                var newLine = prop == entity.Properties.LastOrDefault() ? "" : $"{Environment.NewLine}";
                assertString += @$"            resultDto.Data.{prop.Name}.Should().Be(fake{entity.Name}.{prop.Name});{newLine}";
            }
            var testName = addJwtAuth
                ? @$"Post{entity.Name}ReturnsSuccessCodeAndResourceWithAccurateFields_WithAuth"
                : @$"Post{entity.Name}ReturnsSuccessCodeAndResourceWithAccurateFields";
            var scopes = Utilities.BuildTestAuthorizationString(policies, new List<Endpoint>() { Endpoint.AddRecord }, entity.Name, PolicyType.Scope);
            var clientAuth = addJwtAuth ? @$"

            client.AddAuth(new[] {scopes});" : "";

            return $@"
        [Fact]
        public async Task {testName}()
        {{
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {{
                AllowAutoRedirect = false
            }});
            var fake{entity.Name} = new Fake{Utilities.GetDtoName(entity.Name, Dto.Read)}().Generate();{clientAuth}

            // Act
            var httpResponse = await client.PostAsJsonAsync(""api/{entity.Plural}"", fake{entity.Name})
                .ConfigureAwait(false);

            // Assert
            httpResponse.EnsureSuccessStatusCode();

            var resultDto = JsonConvert.DeserializeObject<Response<{Utilities.GetDtoName(entity.Name, Dto.Read)}>>(await httpResponse.Content.ReadAsStringAsync()
                .ConfigureAwait(false));

            httpResponse.StatusCode.Should().Be(201);
{assertString}
        }}";
        }

        private static string CreateEntityTestUnauthorized(Entity entity)
        {
            return $@"
        [Fact]
        public async Task Post{entity.Plural}_Returns_Unauthorized_Without_Valid_Token()
        {{
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {{
                AllowAutoRedirect = false
            }});
            var fake{entity.Name} = new Fake{Utilities.GetDtoName(entity.Name, Dto.Read)}().Generate();

            // Act
            var httpResponse = await client.PostAsJsonAsync(""api/{entity.Plural}"", fake{entity.Name})
                .ConfigureAwait(false);

            // Assert
            httpResponse.StatusCode.Should().Be(401);
        }}";
        }

        private static string CreateEntityTestForbidden(Entity entity)
        {
            return $@"
        [Fact]
        public async Task Post{entity.Name}_Returns_Forbidden_Without_Proper_Scope()
        {{
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {{
                AllowAutoRedirect = false
            }});
            var fake{entity.Name} = new Fake{Utilities.GetDtoName(entity.Name, Dto.Read)}().Generate();

            client.AddAuth(new[] {{ """" }});

            // Act
            var httpResponse = await client.PostAsJsonAsync(""api/{entity.Plural}"", fake{entity.Name})
                .ConfigureAwait(false);

            // Assert
            httpResponse.StatusCode.Should().Be(403);
        }}";
        }
    }
}
