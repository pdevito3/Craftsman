﻿namespace Craftsman.Commands
{
    using Craftsman.Builders;
    using Craftsman.Exceptions;
    using Craftsman.Helpers;
    using Craftsman.Models;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using static Helpers.ConsoleWriter;
    using Spectre.Console;

    public static class AddEntityPropertyCommand
    {
        public static void Help()
        {
            WriteHelpHeader(@$"Description:");
            WriteHelpText(@$"   While in your project directory, this command will add a new property to an entity.");

            WriteHelpText(Environment.NewLine);
            WriteHelpHeader(@$"Usage:");
            WriteHelpText(@$"   craftsman add:prop [options] [arguments]");

            WriteHelpText(Environment.NewLine);
            WriteHelpHeader(@$"Arguments:");
            WriteHelpText(@$"   -e, -entity        Required. Text. Name of the entity to add the property.
                      Must match the name of the entity file (e.g. `Vet.cs` should
                      be `Vet`)");
            WriteHelpText(@$"   -n, -name          Required. Text. Name of the property to add");
            WriteHelpText(@$"   -t, -type          Required. Text. Data type of the property to add");
            WriteHelpText(@$"   -f, -filter        Optional. Boolean. Determines if the property is filterable");
            WriteHelpText(@$"   -s, -sort          Optional. Boolean. Determines if the property is sortable");
            WriteHelpText(@$"   -k, -foreignkey    Optional. Text. When adding an object linked by a foreign
                      key, use this field to enter the name of the property that
                      acts as the foreign key");

            // add new line back in if adding something with only one line of text after foreign key above
            //WriteHelpText(Environment.NewLine);
            WriteHelpHeader(@$"Options:");
            WriteHelpText(@$"   -h, --help          Display this help message. No Arguments are needed to display the help message.");

            WriteHelpText(Environment.NewLine);
            WriteHelpHeader(@$"Example:");
            WriteHelpText(@$"   craftsman add:prop --entity Vet --name VetName --type string --filter false --sort true");
            WriteHelpText(@$"   craftsman add:prop -e Vet -n VetName -t string -f false -s true");
            WriteHelpText(@$"   craftsman add:prop -e Vet -n VetName -t string");
            WriteHelpText(@$"   craftsman add:prop -e Sale -n Product -t Product -k ProductId");
            WriteHelpText(@$"   craftsman add:prop -e Vet -n AppointmentDate -t DateTime? -f false -s true");
            WriteHelpText(Environment.NewLine);
        }

        public static void Run(string solutionDirectory, string entityName, EntityProperty prop)
        {
            try
            {
                var propList = new List<EntityProperty>() { prop };
                var srcDirectory = Path.Combine(solutionDirectory, "src");
                var testDirectory = Path.Combine(solutionDirectory, "tests");
                Utilities.IsBoundedContextDirectoryGuard(srcDirectory, testDirectory);
                var projectBaseName = Directory.GetParent(srcDirectory).Name;

                EntityModifier.AddEntityProperties(srcDirectory, entityName, propList, projectBaseName);
                DtoModifier.AddPropertiesToDtos(srcDirectory, entityName, propList, projectBaseName);

                WriteHelpHeader($"{Environment.NewLine}The '{prop.Name}' property was successfully added to the '{entityName}' entity and its associated DTOs. Keep up the good work!");
            }
            catch (Exception e)
            {
                if (e is IsNotBoundedContextDirectory)
                {
                    WriteError($"{e.Message}");
                }
                else
                {
                    AnsiConsole.WriteException(e, new ExceptionSettings
                    {
                        Format = ExceptionFormats.ShortenEverything | ExceptionFormats.ShowLinks,
                        Style = new ExceptionStyle
                        {
                            Exception = new Style().Foreground(Color.Grey),
                            Message = new Style().Foreground(Color.White),
                            NonEmphasized = new Style().Foreground(Color.Cornsilk1),
                            Parenthesis = new Style().Foreground(Color.Cornsilk1),
                            Method = new Style().Foreground(Color.Red),
                            ParameterName = new Style().Foreground(Color.Cornsilk1),
                            ParameterType = new Style().Foreground(Color.Red),
                            Path = new Style().Foreground(Color.Red),
                            LineNumber = new Style().Foreground(Color.Cornsilk1),
                        }
                    });
                }
            }
        }
    }
}