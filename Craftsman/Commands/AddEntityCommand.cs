﻿
namespace Craftsman.Commands
{
    using Craftsman.Builders;
    using Craftsman.Builders.Dtos;
    using Craftsman.Builders.Seeders;
    using Craftsman.Builders.Tests.Fakes;
    using Craftsman.Builders.Tests.IntegrationTests;
    using Craftsman.Exceptions;
    using Craftsman.Helpers;
    using Craftsman.Models;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using static Helpers.ConsoleWriter;

    public static class AddEntityCommand
    {
        public static void Help()
        {
            WriteHelpHeader(@$"Description:");
            WriteHelpText(@$"   This command can add one or more new entities to your Wrapt project using a formatted 
   yaml or json file. The input file uses a simplified format from the `new:api` command that only 
   requires a list of one or more entities.{Environment.NewLine}");

            WriteHelpHeader(@$"Usage:");
            WriteHelpText(@$"   craftsman add:entity [options] <filepath>");
            WriteHelpText(@$"   or");
            WriteHelpText(@$"   craftsman add:entities [options] <filepath>");

            WriteHelpText(Environment.NewLine);
            WriteHelpHeader(@$"Arguments:");
            WriteHelpText(@$"   filepath         The full filepath for the yaml or json file that lists the new entities that you want to add to your API.");

            WriteHelpText(Environment.NewLine);
            WriteHelpHeader(@$"Options:");
            WriteHelpText(@$"   -h, --help          Display this help message. No filepath is needed to display the help message.");

            WriteHelpText(Environment.NewLine);
            WriteHelpHeader(@$"Example:");
            WriteHelpText(@$"       craftsman add:entities C:\fullpath\newentity.yaml");
            WriteHelpText(@$"       craftsman add:entities C:\fullpath\newentity.yml");
            WriteHelpText(@$"       craftsman add:entities C:\fullpath\newentity.json");
            WriteHelpText(Environment.NewLine);
        }

        public static void Run(string filePath, string solutionDirectory, IFileSystem fileSystem)
        {
            try
            {
                FileParsingHelper.RunInitialTemplateParsingGuards(filePath);
                var template = FileParsingHelper.GetTemplateFromFile<ApiTemplate>(filePath);

                //var solutionDirectory = Directory.GetCurrentDirectory();
                //var solutionDirectory = @"C:\Users\Paul\Documents\testoutput\MyApi.Mine";
                template = SolutionGuard(solutionDirectory, template);
                template = GetDbContext(solutionDirectory, template);

                WriteHelpText($"Your template file was parsed successfully.");

                FileParsingHelper.RunPrimaryKeyGuard(template.Entities);

                // add all files based on the given template config
                RunEntityBuilders(solutionDirectory, template, fileSystem);

                WriteHelpHeader($"{Environment.NewLine}Your entities have been successfully added. Keep up the good work!");
            }
            catch (Exception e)
            {
                if (e is FileAlreadyExistsException
                    || e is DirectoryAlreadyExistsException
                    || e is InvalidSolutionNameException
                    || e is FileNotFoundException
                    || e is InvalidDbProviderException
                    || e is InvalidFileTypeException
                    || e is SolutionNotFoundException)
                {
                    WriteError($"{e.Message}");
                }
                else
                    WriteError($"An unhandled exception occurred when running the API command.\nThe error details are: \n{e.Message}");
            }
        }

        private static void RunEntityBuilders(string solutionDirectory, ApiTemplate template, IFileSystem fileSystem)
        {
            //entities
            foreach (var entity in template.Entities)
            {
                EntityBuilder.CreateEntity(solutionDirectory, entity, "EntityBrokenHere", fileSystem);
                DtoBuilder.CreateDtos(solutionDirectory, entity, "EntityBrokenHere");

                ValidatorBuilder.CreateValidators(solutionDirectory, "EntityBrokenHere", entity);
                ProfileBuilder.CreateProfile(solutionDirectory, entity, "EntityBrokenHere");

                ControllerBuilder.CreateController(solutionDirectory, entity, template.SwaggerConfig.AddSwaggerComments, template.AuthorizationSettings.Policies);
                InfrastructureServiceRegistrationModifier.AddPolicies(solutionDirectory, template.AuthorizationSettings.Policies, "EntityBrokenHere");

                FakesBuilder.CreateFakes(solutionDirectory, template.SolutionName, entity);
                //ReadTestBuilder.CreateEntityReadTests(testDirectory, template.SolutionName, entity, template.DbContext.ContextName);
                //DeleteTestBuilder.DeleteEntityWriteTests(testDirectory, entity, template.SolutionName, template.DbContext.ContextName);
                //WriteTestBuilder.CreateEntityWriteTests(testDirectory, entity, template.SolutionName, template.DbContext.ContextName);
                //GetIntegrationTestBuilder.CreateEntityGetTests(solutionDirectory, template.SolutionName, entity, template.DbContext.ContextName, template.AuthorizationSettings.Policies, "EntityBrokenHere");
                //PostIntegrationTestBuilder.CreateEntityWriteTests(solutionDirectory, entity, template.SolutionName, template.AuthorizationSettings.Policies, "EntityBrokenHere");
                //UpdateIntegrationTestBuilder.CreateEntityUpdateTests(solutionDirectory, entity, template.SolutionName, template.DbContext.ContextName, template.AuthorizationSettings.Policies, "EntityBrokenHere");
                //DeleteIntegrationTestBuilder.CreateEntityDeleteTests(solutionDirectory, entity, template.SolutionName, template.DbContext.ContextName, template.AuthorizationSettings.Policies, "EntityBrokenHere");
            }

            //seeders & dbsets
            SeederModifier.AddSeeders(solutionDirectory, template.Entities, template.DbContext.ContextName, "EntityBrokenHere");
            DbContextModifier.AddDbSet(solutionDirectory, template.Entities, template.DbContext.ContextName, "EntityBrokenHere");
        }

        private static string GetSlnFile(string filePath)
        {
            // make sure i'm in the sln directory -- should i add an accelerate.config.yaml file to the root?
            return Directory.GetFiles(filePath, "*.sln").FirstOrDefault();
        }

        private static ApiTemplate SolutionGuard(string solutionDirectory, ApiTemplate template)
        {
            var slnName = GetSlnFile(solutionDirectory);
            template.SolutionName = Path.GetFileNameWithoutExtension(slnName) ?? throw new SolutionNotFoundException();

            return template;
        }

        private static ApiTemplate GetDbContext(string solutionDirectory, ApiTemplate template)
        {
            var classPath = ClassPathHelper.DbContextClassPath(solutionDirectory, $"", "EntityBrokenHere");
            var contextClass = Directory.GetFiles(classPath.FullClassPath, "*.cs").FirstOrDefault();

            template.DbContext.ContextName = Path.GetFileNameWithoutExtension(contextClass);
            return template;
        }
    }
}
