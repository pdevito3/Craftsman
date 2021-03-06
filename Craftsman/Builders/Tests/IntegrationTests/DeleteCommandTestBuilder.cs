﻿namespace Craftsman.Builders.Tests.IntegrationTests
{
    using Craftsman.Exceptions;
    using Craftsman.Helpers;
    using Craftsman.Models;
    using System.IO;
    using System.Text;

    public class DeleteCommandTestBuilder
    {
        public static void CreateTests(string solutionDirectory, Entity entity, string projectBaseName)
        {
            var classPath = ClassPathHelper.FeatureTestClassPath(solutionDirectory, $"Delete{entity.Name}CommandTests.cs", entity.Name, projectBaseName);

            if (!Directory.Exists(classPath.ClassDirectory))
                Directory.CreateDirectory(classPath.ClassDirectory);

            if (File.Exists(classPath.FullClassPath))
                throw new FileAlreadyExistsException(classPath.FullClassPath);

            using (FileStream fs = File.Create(classPath.FullClassPath))
            {
                var data = WriteTestFileText(solutionDirectory, classPath, entity, projectBaseName);
                fs.Write(Encoding.UTF8.GetBytes(data));
            }
        }

        private static string WriteTestFileText(string solutionDirectory, ClassPath classPath, Entity entity, string projectBaseName)
        {
            var featureName = Utilities.DeleteEntityFeatureClassName(entity.Name);
            var testFixtureName = Utilities.GetIntegrationTestFixtureName();
            var commandName = Utilities.CommandDeleteName(entity.Name);
            var badId = entity.PrimaryKeyProperty.Name;

            var testUtilClassPath = ClassPathHelper.IntegrationTestUtilitiesClassPath(solutionDirectory, projectBaseName, "");
            var fakerClassPath = ClassPathHelper.TestFakesClassPath(solutionDirectory, "", entity.Name, projectBaseName);
            var featuresClassPath = ClassPathHelper.FeaturesClassPath(solutionDirectory, featureName, entity.Plural, projectBaseName);

            return @$"namespace {classPath.ClassNamespace}
{{
    using {fakerClassPath.ClassNamespace};
    using {testUtilClassPath.ClassNamespace};
    using FluentAssertions;
    using Microsoft.EntityFrameworkCore;
    using NUnit.Framework;
    using System.Collections.Generic;
    using System;
    using System.Threading.Tasks;
    using {featuresClassPath.ClassNamespace};
    using static {testFixtureName};

    public class {commandName}Tests : TestBase
    {{
        {GetDeleteTest(commandName, entity, featureName)}{GetDeleteWithoutKeyTest(commandName, entity, featureName)}
    }}
}}";
        }

        private static string GetDeleteTest(string commandName, Entity entity, string featureName)
        {
            var fakeEntity = Utilities.FakerName(entity.Name);
            var fakeEntityVariableName = $"fake{entity.Name}One";
            var lowercaseEntityName = entity.Name.LowercaseFirstLetter();
            var lowercaseEntityPluralName = entity.Plural.LowercaseFirstLetter();
            var pkName = entity.PrimaryKeyProperty.Name;
            var lowercaseEntityPk = pkName.LowercaseFirstLetter();

            return $@"[Test]
        public async Task {commandName}_Deletes_{entity.Name}_From_Db()
        {{
            // Arrange
            var {fakeEntityVariableName} = new {fakeEntity} {{ }}.Generate();
            await InsertAsync({fakeEntityVariableName});
            var {lowercaseEntityName} = await ExecuteDbContextAsync(db => db.{entity.Plural}.SingleOrDefaultAsync());
            var {lowercaseEntityPk} = {lowercaseEntityName}.{pkName};

            // Act
            var command = new {featureName}.{commandName}({lowercaseEntityPk});
            await SendAsync(command);
            var {lowercaseEntityPluralName} = await ExecuteDbContextAsync(db => db.{entity.Plural}.ToListAsync());

            // Assert
            {lowercaseEntityPluralName}.Count.Should().Be(0);
        }}";
        }

        private static string GetDeleteWithoutKeyTest(string commandName, Entity entity, string featureName)
        {
            var badId = Utilities.GetRandomId(entity.PrimaryKeyProperty.Type);

            return badId == "" ? "" : $@"

        [Test]
        public async Task {commandName}_Throws_KeyNotFoundException_When_Record_Does_Not_Exist()
        {{
            // Arrange
            var badId = {badId};

            // Act
            var command = new {featureName}.{commandName}(badId);
            Func<Task> act = () => SendAsync(command);

            // Assert
            act.Should().Throw<KeyNotFoundException>();
        }}";
        }
    }
}