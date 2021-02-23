﻿namespace Craftsman.Builders
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

    public static class ControllerBuilder
    {
        public static void CreateController(string solutionDirectory, Entity entity, bool AddSwaggerComments, List<Policy> policies)
        {
            try
            {
                var classPath = ClassPathHelper.ControllerClassPath(solutionDirectory, $"{Utilities.GetControllerName(entity.Plural)}.cs");

                if (!Directory.Exists(classPath.ClassDirectory))
                    Directory.CreateDirectory(classPath.ClassDirectory);

                if (File.Exists(classPath.FullClassPath))
                    throw new FileAlreadyExistsException(classPath.FullClassPath);

                using (FileStream fs = File.Create(classPath.FullClassPath))
                {
                    var data = GetControllerFileText(classPath.ClassNamespace, entity, AddSwaggerComments, policies);
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

        public static string GetControllerFileText(string classNamespace, Entity entity, bool AddSwaggerComments, List<Policy> policies)
        {
            var lowercaseEntityVariable = entity.Name.LowercaseFirstLetter();
            var lowercaseEntityVariableSingularDto = $@"{entity.Name.LowercaseFirstLetter()}Dto";
            var lowercaseEntityVariablePluralDto = $@"{entity.Plural.LowercaseFirstLetter()}Dto";
            var entityName = entity.Name;
            var entityNamePlural = entity.Plural;
            var readDto = Utilities.GetDtoName(entityName, Dto.Read);
            var readParamDto = Utilities.GetDtoName(entityName, Dto.ReadParamaters);
            var creationDto = Utilities.GetDtoName(entityName, Dto.Creation);
            var updateDto = Utilities.GetDtoName(entityName, Dto.Update);
            var primaryKeyProp = entity.PrimaryKeyProperty;
            var getListMethodName = Utilities.GetRepositoryListMethodName(entity.Plural);
            var pkPropertyType = primaryKeyProp.Type;
            var listResponse = $@"Response<IEnumerable<{readDto}>>";
            var singleResponse = $@"Response<{readDto}>";
            var getListEndpointName = entity.Name == entity.Plural ? $@"Get{entityNamePlural}List" : $@"Get{entityNamePlural}";
            var getRecordEndpointName = entity.Name == entity.Plural ? $@"Get{entityNamePlural}Record" : $@"Get{entity.Name}";
            var endpointBase = Utilities.EndpointBaseGenerator(entityNamePlural);
            var getListAuthorizations = BuildAuthorizations(policies, Endpoint.GetList, entity.Name);
            var getRecordAuthorizations = BuildAuthorizations(policies, Endpoint.GetRecord, entity.Name);
            var addRecordAuthorizations = BuildAuthorizations(policies, Endpoint.AddRecord, entity.Name);
            var updateRecordAuthorizations = BuildAuthorizations(policies, Endpoint.UpdateRecord, entity.Name);
            var updatePartialAuthorizations = BuildAuthorizations(policies, Endpoint.UpdatePartial, entity.Name);
            var deleteRecordAuthorizations = BuildAuthorizations(policies, Endpoint.DeleteRecord, entity.Name);

            return @$"namespace {classNamespace}
{{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using AutoMapper;
    using FluentValidation.AspNetCore;
    using Application.Dtos.{entityName};
    using Application.Interfaces.{entityName};
    using Application.Validation.{entityName};
    using Domain.Entities;
    using Microsoft.AspNetCore.JsonPatch;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
    using System.Threading.Tasks;
    using Application.Wrappers;

    [ApiController]
    [Route(""{endpointBase}"")]
    [ApiVersion(""1.0"")]
    public class {entityNamePlural}Controller: Controller
    {{
        private readonly {Utilities.GetRepositoryName(entity.Name, true)} _{lowercaseEntityVariable}Repository;
        private readonly IMapper _mapper;

        public {entityNamePlural}Controller({Utilities.GetRepositoryName(entity.Name, true)} {lowercaseEntityVariable}Repository
            , IMapper mapper)
        {{
            _{lowercaseEntityVariable}Repository = {lowercaseEntityVariable}Repository ??
                throw new ArgumentNullException(nameof({lowercaseEntityVariable}Repository));
            _mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
        }}
        {GetSwaggerComments_GetList(entity, AddSwaggerComments, listResponse, getListAuthorizations.Length > 0)}{getListAuthorizations}
        [Consumes(""application/json"")]
        [Produces(""application/json"")]
        [HttpGet(Name = ""{getListEndpointName}"")]
        public async Task<IActionResult> Get{entityNamePlural}([FromQuery] {readParamDto} {lowercaseEntityVariable}ParametersDto)
        {{
            var {lowercaseEntityVariable}sFromRepo = await _{lowercaseEntityVariable}Repository.{getListMethodName}({lowercaseEntityVariable}ParametersDto);

            var paginationMetadata = new
            {{
                totalCount = {lowercaseEntityVariable}sFromRepo.TotalCount,
                pageSize = {lowercaseEntityVariable}sFromRepo.PageSize,
                currentPageSize = {lowercaseEntityVariable}sFromRepo.CurrentPageSize,
                currentStartIndex = {lowercaseEntityVariable}sFromRepo.CurrentStartIndex,
                currentEndIndex = {lowercaseEntityVariable}sFromRepo.CurrentEndIndex,
                pageNumber = {lowercaseEntityVariable}sFromRepo.PageNumber,
                totalPages = {lowercaseEntityVariable}sFromRepo.TotalPages,
                hasPrevious = {lowercaseEntityVariable}sFromRepo.HasPrevious,
                hasNext = {lowercaseEntityVariable}sFromRepo.HasNext
            }};

            Response.Headers.Add(""X-Pagination"",
                JsonSerializer.Serialize(paginationMetadata));

            var {lowercaseEntityVariablePluralDto} = _mapper.Map<IEnumerable<{entityName}Dto>>({lowercaseEntityVariable}sFromRepo);
            var response = new {listResponse}({lowercaseEntityVariablePluralDto});

            return Ok(response);
        }}
        {GetSwaggerComments_GetRecord(entity, AddSwaggerComments, singleResponse, getRecordAuthorizations.Length > 0)}{getRecordAuthorizations}
        [Produces(""application/json"")]
        [HttpGet(""{{{lowercaseEntityVariable}Id}}"", Name = ""{getRecordEndpointName}"")]
        public async Task<ActionResult<{readDto}>> Get{entityName}({pkPropertyType} {lowercaseEntityVariable}Id)
        {{
            var {lowercaseEntityVariable}FromRepo = await _{lowercaseEntityVariable}Repository.Get{entityName}Async({lowercaseEntityVariable}Id);

            if ({lowercaseEntityVariable}FromRepo == null)
            {{
                return NotFound();
            }}

            var {lowercaseEntityVariableSingularDto} = _mapper.Map<{readDto}>({lowercaseEntityVariable}FromRepo);
            var response = new {singleResponse}({lowercaseEntityVariableSingularDto});

            return Ok(response);
        }}
        {GetSwaggerComments_CreateRecord(entity, AddSwaggerComments, singleResponse, addRecordAuthorizations.Length > 0)}{addRecordAuthorizations}
        [Consumes(""application/json"")]
        [Produces(""application/json"")]
        [HttpPost]
        public async Task<ActionResult<{readDto}>> Add{entityName}([FromBody]{creationDto} {lowercaseEntityVariable}ForCreation)
        {{
            var validationResults = new {entityName}ForCreationDtoValidator().Validate({lowercaseEntityVariable}ForCreation);
            validationResults.AddToModelState(ModelState, null);

            if (!ModelState.IsValid)
            {{
                return BadRequest(new ValidationProblemDetails(ModelState));
                //return ValidationProblem();
            }}

            var {lowercaseEntityVariable} = _mapper.Map<{entityName}>({lowercaseEntityVariable}ForCreation);
            await _{lowercaseEntityVariable}Repository.Add{entityName}({lowercaseEntityVariable});
            var saveSuccessful = await _{lowercaseEntityVariable}Repository.SaveAsync();

            if(saveSuccessful)
            {{
                var {lowercaseEntityVariable}FromRepo = await _{lowercaseEntityVariable}Repository.Get{entityName}Async({lowercaseEntityVariable}.{entity.PrimaryKeyProperty.Name});
                var {lowercaseEntityVariableSingularDto} = _mapper.Map<{readDto}>({lowercaseEntityVariable}FromRepo);
                var response = new {singleResponse}({lowercaseEntityVariableSingularDto});
                
                return CreatedAtRoute(""Get{entityName}"",
                    new {{ {lowercaseEntityVariable}Dto.{primaryKeyProp.Name} }},
                    response);
            }}

            return StatusCode(500);
        }}
        {GetSwaggerComments_DeleteRecord(entity, AddSwaggerComments, deleteRecordAuthorizations.Length > 0)}{deleteRecordAuthorizations}
        [Produces(""application/json"")]
        [HttpDelete(""{{{lowercaseEntityVariable}Id}}"")]
        public async Task<ActionResult> Delete{entityName}({pkPropertyType} {lowercaseEntityVariable}Id)
        {{
            var {lowercaseEntityVariable}FromRepo = await _{lowercaseEntityVariable}Repository.Get{entityName}Async({lowercaseEntityVariable}Id);

            if ({lowercaseEntityVariable}FromRepo == null)
            {{
                return NotFound();
            }}

            _{lowercaseEntityVariable}Repository.Delete{entityName}({lowercaseEntityVariable}FromRepo);
            await _{lowercaseEntityVariable}Repository.SaveAsync();

            return NoContent();
        }}
        {GetSwaggerComments_PutRecord(entity, AddSwaggerComments, updateRecordAuthorizations.Length > 0)}{updateRecordAuthorizations}
        [Produces(""application/json"")]
        [HttpPut(""{{{lowercaseEntityVariable}Id}}"")]
        public async Task<IActionResult> Update{entityName}({pkPropertyType} {lowercaseEntityVariable}Id, {updateDto} {lowercaseEntityVariable})
        {{
            var {lowercaseEntityVariable}FromRepo = await _{lowercaseEntityVariable}Repository.Get{entityName}Async({lowercaseEntityVariable}Id);

            if ({lowercaseEntityVariable}FromRepo == null)
            {{
                return NotFound();
            }}

            var validationResults = new {entityName}ForUpdateDtoValidator().Validate({lowercaseEntityVariable});
            validationResults.AddToModelState(ModelState, null);

            if (!ModelState.IsValid)
            {{
                return BadRequest(new ValidationProblemDetails(ModelState));
                //return ValidationProblem();
            }}

            _mapper.Map({lowercaseEntityVariable}, {lowercaseEntityVariable}FromRepo);
            _{lowercaseEntityVariable}Repository.Update{entityName}({lowercaseEntityVariable}FromRepo);

            await _{lowercaseEntityVariable}Repository.SaveAsync();

            return NoContent();
        }}
        {GetSwaggerComments_PatchRecord(entity, AddSwaggerComments, updatePartialAuthorizations.Length > 0)}{updatePartialAuthorizations}
        [Consumes(""application/json"")]
        [Produces(""application/json"")]
        [HttpPatch(""{{{lowercaseEntityVariable}Id}}"")]
        public async Task<IActionResult> PartiallyUpdate{entityName}({pkPropertyType} {lowercaseEntityVariable}Id, JsonPatchDocument<{updateDto}> patchDoc)
        {{
            if (patchDoc == null)
            {{
                return BadRequest();
            }}

            var existing{entityName} = await _{lowercaseEntityVariable}Repository.Get{entityName}Async({lowercaseEntityVariable}Id);

            if (existing{entityName} == null)
            {{
                return NotFound();
            }}

            var {lowercaseEntityVariable}ToPatch = _mapper.Map<{updateDto}>(existing{entityName}); // map the {lowercaseEntityVariable} we got from the database to an updatable {lowercaseEntityVariable} model
            patchDoc.ApplyTo({lowercaseEntityVariable}ToPatch, ModelState); // apply patchdoc updates to the updatable {lowercaseEntityVariable}

            if (!TryValidateModel({lowercaseEntityVariable}ToPatch))
            {{
                return ValidationProblem(ModelState);
            }}

            _mapper.Map({lowercaseEntityVariable}ToPatch, existing{entityName}); // apply updates from the updatable {lowercaseEntityVariable} to the db entity so we can apply the updates to the database
            _{lowercaseEntityVariable}Repository.Update{entityName}(existing{entityName}); // apply business updates to data if needed

            await _{lowercaseEntityVariable}Repository.SaveAsync(); // save changes in the database

            return NoContent();
        }}
    }}
}}";
        }

        private static string GetSwaggerComments_GetList(Entity entity, bool buildComments, string listResponse, bool hasAuthentications)
        {
            var authResponses = GetAuthResponses(hasAuthentications);
            var authCommentResponses = GetAuthCommentResponses(hasAuthentications);

            if (buildComments)
                return $@"
        /// <summary>
        /// Gets a list of all {entity.Plural}.
        /// </summary>
        /// <response code=""200"">{entity.Name} list returned successfully.</response>
        /// <response code=""400"">{entity.Name} has missing/invalid values.</response>{authCommentResponses}
        /// <response code=""500"">There was an error on the server while creating the {entity.Name}.</response>
        /// <remarks>
        /// Requests can be narrowed down with a variety of query string values:
        /// ## Query String Parameters
        /// - **PageNumber**: An integer value that designates the page of records that should be returned.
        /// - **PageSize**: An integer value that designates the number of records returned on the given page that you would like to return. This value is capped by the internal MaxPageSize.
        /// - **SortOrder**: A comma delimited ordered list of property names to sort by. Adding a `-` before the name switches to sorting descendingly.
        /// - **Filters**: A comma delimited list of fields to filter by formatted as `{{Name}}{{Operator}}{{Value}}` where
        ///     - {{Name}} is the name of a filterable property. You can also have multiple names (for OR logic) by enclosing them in brackets and using a pipe delimiter, eg. `(LikeCount|CommentCount)>10` asks if LikeCount or CommentCount is >10
        ///     - {{Operator}} is one of the Operators below
        ///     - {{Value}} is the value to use for filtering. You can also have multiple values (for OR logic) by using a pipe delimiter, eg.`Title@= new|hot` will return posts with titles that contain the text ""new"" or ""hot""
        ///
        ///    | Operator | Meaning                       | Operator  | Meaning                                      |
        ///    | -------- | ----------------------------- | --------- | -------------------------------------------- |
        ///    | `==`     | Equals                        |  `!@=`    | Does not Contains                            |
        ///    | `!=`     | Not equals                    |  `!_=`    | Does not Starts with                         |
        ///    | `>`      | Greater than                  |  `@=*`    | Case-insensitive string Contains             |
        ///    | `&lt;`   | Less than                     |  `_=*`    | Case-insensitive string Starts with          |
        ///    | `>=`     | Greater than or equal to      |  `==*`    | Case-insensitive string Equals               |
        ///    | `&lt;=`  | Less than or equal to         |  `!=*`    | Case-insensitive string Not equals           |
        ///    | `@=`     | Contains                      |  `!@=*`   | Case-insensitive string does not Contains    |
        ///    | `_=`     | Starts with                   |  `!_=*`   | Case-insensitive string does not Starts with |
        /// </remarks>
        [ProducesResponseType(typeof({listResponse}), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]{authResponses}
        [ProducesResponseType(500)]";

            return "";
        }

        private static string GetSwaggerComments_GetRecord(Entity entity, bool buildComments, string singleResponse, bool hasAuthentications)
        {
            var authResponses = GetAuthResponses(hasAuthentications);
            var authCommentResponses = GetAuthCommentResponses(hasAuthentications);

            if (buildComments)
                return $@"
        /// <summary>
        /// Gets a single {entity.Name} by ID.
        /// </summary>
        /// <response code=""200"">{entity.Name} record returned successfully.</response>
        /// <response code=""400"">{entity.Name} has missing/invalid values.</response>{authCommentResponses}
        /// <response code=""500"">There was an error on the server while creating the {entity.Name}.</response>
        [ProducesResponseType(typeof({singleResponse}), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]{authResponses}
        [ProducesResponseType(500)]";

            return "";
        }

        private static string GetSwaggerComments_CreateRecord(Entity entity, bool buildComments, string singleResponse, bool hasAuthentications)
        {
            var authResponses = GetAuthResponses(hasAuthentications);
            var authCommentResponses = GetAuthCommentResponses(hasAuthentications);
            if (buildComments)
                return $@"
        /// <summary>
        /// Creates a new {entity.Name} record.
        /// </summary>
        /// <response code=""201"">{entity.Name} created.</response>
        /// <response code=""400"">{entity.Name} has missing/invalid values.</response>{authCommentResponses}
        /// <response code=""500"">There was an error on the server while creating the {entity.Name}.</response>
        [ProducesResponseType(typeof({singleResponse}), 201)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]{authResponses}
        [ProducesResponseType(500)]";

            return "";
        }

        private static string GetSwaggerComments_DeleteRecord(Entity entity, bool buildComments, bool hasAuthentications)
        {
            var authResponses = GetAuthResponses(hasAuthentications);
            var authCommentResponses = GetAuthCommentResponses(hasAuthentications);
            if (buildComments)
                return $@"
        /// <summary>
        /// Deletes an existing {entity.Name} record.
        /// </summary>
        /// <response code=""201"">{entity.Name} deleted.</response>
        /// <response code=""400"">{entity.Name} has missing/invalid values.</response>{authCommentResponses}
        /// <response code=""500"">There was an error on the server while creating the {entity.Name}.</response>
        [ProducesResponseType(201)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]{authResponses}
        [ProducesResponseType(500)]";

            return "";
        }

        private static string GetSwaggerComments_PatchRecord(Entity entity, bool buildComments, bool hasAuthentications)
        {
            var authResponses = GetAuthResponses(hasAuthentications);
            var authCommentResponses = GetAuthCommentResponses(hasAuthentications);
            if (buildComments)
                return $@"
        /// <summary>
        /// Updates specific properties on an existing {entity.Name}.
        /// </summary>
        /// <response code=""201"">{entity.Name} updated.</response>
        /// <response code=""400"">{entity.Name} has missing/invalid values.</response>{authCommentResponses}
        /// <response code=""500"">There was an error on the server while creating the {entity.Name}.</response>
        [ProducesResponseType(201)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]{authResponses}
        [ProducesResponseType(500)]";

            return "";
        }

        private static string GetSwaggerComments_PutRecord(Entity entity, bool buildComments, bool hasAuthentications)
        {
            var authResponses = GetAuthResponses(hasAuthentications);
            var authCommentResponses = GetAuthCommentResponses(hasAuthentications);
            if (buildComments)
                return $@"
        /// <summary>
        /// Updates an entire existing {entity.Name}.
        /// </summary>
        /// <response code=""201"">{entity.Name} updated.</response>
        /// <response code=""400"">{entity.Name} has missing/invalid values.</response>{authCommentResponses}
        /// <response code=""500"">There was an error on the server while creating the {entity.Name}.</response>
        [ProducesResponseType(201)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]{authResponses}
        [ProducesResponseType(500)]";

            return "";
        }

        private static string BuildAuthorizations(List<Policy> policies, Endpoint endpoint, string entityName)
        {
            var results = Utilities.GetEndpointPolicies(policies, endpoint, entityName);

            var authorizations = "";
            foreach (var result in results)
            {
                if (result.PolicyType == Enum.GetName(typeof(PolicyType), PolicyType.Scope))
                    //|| result.PolicyType == Enum.GetName(typeof(PolicyType), PolicyType.Claim))
                {
                    authorizations += $@"{Environment.NewLine}        [Authorize(Policy = ""{result.Name}"")]";
                }
                else
                {
                    authorizations += $@"{Environment.NewLine}        [Authorize(Roles = ""{result.Name}"")]";
                }
            }

            return authorizations;
        }

        private static string GetAuthResponses(bool hasAuthentications)
        {
            var authResponses = "";
            if (hasAuthentications)
            {
                authResponses = $@"
        [ProducesResponseType(401)] 
        [ProducesResponseType(403)]";
            }

            return authResponses;
        }

        private static string GetAuthCommentResponses(bool hasAuthentications)
        {
            var authResponseComments = "";
            if (hasAuthentications)
            {
                authResponseComments = $@"
        /// <response code=""401"">This request was not able to be authenticated.</response>
        /// <response code=""403"">The required permissions to access this resource were not present in the given request.</response>";
            }

            return authResponseComments;

        }
    }
}
