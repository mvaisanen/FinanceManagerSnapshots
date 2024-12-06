using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Logging;
using Server.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Server.Services
{
    public class GenTypescriptSdk
    {
        private readonly IApiDescriptionGroupCollectionProvider _apiDescriptionsProvider;
        private readonly ILogger<GenTypescriptSdk> _logger;

        public GenTypescriptSdk(IApiDescriptionGroupCollectionProvider apiDescriptionsProvider, ILogger<GenTypescriptSdk> logger)
        {
            _apiDescriptionsProvider = apiDescriptionsProvider;
            _logger = logger;
            _logger.LogDebug("testi");
        }

        public void Generate()
        {
            _logger.LogDebug("Generating sdk...");
            //Imports etc

            var headersDict = new Dictionary<string, StringBuilder>();
            var constantsDict = new Dictionary<string, StringBuilder>();
            var interfacesDict = new Dictionary<string, StringBuilder>();
            var actionCreatorsDict = new Dictionary<string, StringBuilder>();
            var actionsDict = new Dictionary<string, StringBuilder>();
            var dtoDict = new Dictionary<string, string>();
            var neededDtosDict = new Dictionary<string, List<string>>(); //group, list of needed dto names
            var groups = new List<string>();

            foreach (var grp in _apiDescriptionsProvider.ApiDescriptionGroups.Items)
            {
                _logger.LogDebug($"Group name: {grp.GroupName}");
                
                var constants = new StringBuilder();
                var headers = new StringBuilder();
                var interfaces = new StringBuilder();
                var actionCreators = new StringBuilder();
                var actions = new StringBuilder();
                var neededDtos = new List<string>();
                string combinedInterface = "";

                headers.AppendLine("//Auto-generated TypeScript SDK. Do not edit this, changes will be overwritten by server project.");
                headers.AppendLine("import { Dispatch } from 'redux';");
                headers.AppendLine("import { fetchWithAuth } from '../../common/helpers/fetchWithAuth';");
                //headers.AppendLine($"import {{ {grp.GroupName}DTO }} from '../../models/{grp.GroupName}DTO';"); //TODO: Auto-generate DTOs
                //headers.AppendLine($"import {{ {grp.GroupName} as {grp.GroupName}DTO }} from '../../models/{grp.GroupName}';");

                foreach (var act in grp.Items)
                {
                    var method = act.HttpMethod;
                    var PostPut = method.ToUpper() == "POST" || method.ToUpper() == "PUT";
                    var Delete = method.ToUpper() == "DELETE";
                    // Parametrit
                    var pms = string.Join(",", act.ParameterDescriptions.Select(t => t.Name));

                    // Ohjaimen HTTP kysely
                    _logger.LogDebug($"{act.HttpMethod}  {act.RelativePath}  {pms}");

                    var nameRegex = new Regex(@".([^.]+) ");
                    var nameMatch = nameRegex.Match(act.ActionDescriptor.DisplayName);
                    if (!nameMatch.Success)
                    {
                        _logger.LogError($"Unable to determine method name for ts sdk. Action display name: {act.ActionDescriptor.DisplayName}");
                        break;
                    }
                    var name = nameMatch.Groups[1].Value;

                    //action types
                    constants.AppendLine($"export const {act.HttpMethod.ToUpper()}_{name.ToUpper()}_BEGIN = '{act.HttpMethod.ToUpper()}_{name.ToUpper()}_BEGIN';");
                    constants.AppendLine($"export type {act.HttpMethod.ToUpper()}_{name.ToUpper()}_BEGIN = typeof {act.HttpMethod.ToUpper()}_{name.ToUpper()}_BEGIN;");
                    constants.AppendLine($"export const {act.HttpMethod.ToUpper()}_{name.ToUpper()}_SUCCESS = '{act.HttpMethod.ToUpper()}_{name.ToUpper()}_SUCCESS';");
                    constants.AppendLine($"export type {act.HttpMethod.ToUpper()}_{name.ToUpper()}_SUCCESS = typeof {act.HttpMethod.ToUpper()}_{name.ToUpper()}_SUCCESS;");
                    constants.AppendLine($"export const {act.HttpMethod.ToUpper()}_{name.ToUpper()}_ERROR = '{act.HttpMethod.ToUpper()}_{name.ToUpper()}_ERROR';");
                    constants.AppendLine($"export type {act.HttpMethod.ToUpper()}_{name.ToUpper()}_ERROR = typeof {act.HttpMethod.ToUpper()}_{name.ToUpper()}_ERROR;");


                    //interfaces
                    var beginInterface = $"I{act.HttpMethod.ToFirstLetterUpper()}{name}Begin";
                    var successInterface = $"I{act.HttpMethod.ToFirstLetterUpper()}{name}Success";
                    var errorInterface = $"I{act.HttpMethod.ToFirstLetterUpper()}{name}Error";

                    interfaces.AppendLine($"export interface {beginInterface} {"{"}");
                    interfaces.AppendLine($"    type: {act.HttpMethod}_{name.ToUpper()}_BEGIN;");
                    if (PostPut || Delete)
                    {
                        interfaces.AppendLine($"    itemId: number;");
                    }
                    interfaces.AppendLine("}");
                    interfaces.AppendLine($"export interface {successInterface} {"{"}");
                    interfaces.AppendLine($"    type: {act.HttpMethod}_{name.ToUpper()}_SUCCESS;");

                    var respType = act.SupportedResponseTypes.First().Type;
                    var respTypeName = "";

                    //Response type handling. Determine type (todo: convert to typescript), add to dtos if needed
                    if (respType.IsGenericType && (respType.GetGenericTypeDefinition() == typeof(List<>)))
                    {
                        respTypeName = $"{respType.GetGenericArguments().Single().Name}[]";
                        if (!dtoDict.Keys.Any(k => k == respType.GetGenericArguments().Single().Name))
                            dtoDict.Add(respType.GetGenericArguments().Single().Name, GenerateDto(respType.GetGenericArguments().Single()));
                        if (!neededDtos.Any(d => d == respType.GetGenericArguments().Single().Name))
                            neededDtos.Add(respType.GetGenericArguments().Single().Name);
                        
                    }
                    else
                    {
                        respTypeName = $"{ respType.Name.ToString()}";
                        if (respTypeName.ToLower() != "string") //Don't generate DTOs for string etc. Needs multiuse check...
                        {
                            if (!dtoDict.Keys.Any(k => k == respTypeName))
                                dtoDict.Add(respTypeName, GenerateDto(respType));
                            if (!neededDtos.Any(d => d == respTypeName))
                                neededDtos.Add(respTypeName);
                        }
                    }

                    interfaces.AppendLine($"    result: {respTypeName};");
                    interfaces.AppendLine("}");
                    interfaces.AppendLine($"export interface {errorInterface} {"{"}");
                    interfaces.AppendLine($"    type: {act.HttpMethod}_{name.ToUpper()}_ERROR;");
                    interfaces.AppendLine($"    error: string;");
                    interfaces.AppendLine("}");
                    interfaces.AppendLine();

                    // Combined action interface, ie "WatchlistAction"
                    if (combinedInterface != "")
                        combinedInterface += " | ";
                    combinedInterface += beginInterface + " | " + successInterface + " | " + errorInterface;

                    //action creators
                    if (PostPut || Delete)
                        actionCreators.AppendLine($"function {beginInterface.Substring(1)}(id: number): {beginInterface} {"{"}");
                    else
                        actionCreators.AppendLine($"function {beginInterface.Substring(1)}(): {beginInterface} {"{"}");
                    actionCreators.AppendLine("   return {");
                    actionCreators.AppendLine($"      type: {act.HttpMethod}_{name.ToUpper()}_BEGIN");
                    if (PostPut || Delete)
                    {
                        actionCreators.Append(",");
                        actionCreators.AppendLine($"      itemId: id");
                    }
                    actionCreators.AppendLine("   }");
                    actionCreators.AppendLine("} \r\n");

                    actionCreators.AppendLine($"function {successInterface.Substring(1)}(payload: {respTypeName}): {successInterface} {"{"}");
                    actionCreators.AppendLine("   return {");
                    actionCreators.AppendLine($"      type: {act.HttpMethod}_{name.ToUpper()}_SUCCESS,");
                    actionCreators.AppendLine($"      result: payload");
                    actionCreators.AppendLine("   }");
                    actionCreators.AppendLine("} \r\n");

                    actionCreators.AppendLine($"function {errorInterface.Substring(1)}(error: string): {errorInterface} {"{"}");
                    actionCreators.AppendLine("   return {");
                    actionCreators.AppendLine($"      type: {act.HttpMethod}_{name.ToUpper()}_ERROR,");
                    actionCreators.AppendLine($"      error: error");
                    actionCreators.AppendLine("   }");
                    actionCreators.AppendLine("} \r\n");

                    //actions... experimental
                    
                    actions.AppendLine("\r\n//Auto-generated actions, lets see if these work...");

                    ApiParameterDescription bodyParam = null;
                    if (PostPut)
                    {
                        var parameters = act.ParameterDescriptions;
                        bodyParam = parameters.Where(p => p.Source.Id.ToLower() == "body").Single();
                        //headers.AppendLine($"import {{ {bodyParam.Type.Name} }} from '../../models/{bodyParam.Type.Name}';");
                        actions.AppendLine($"export function action{act.HttpMethod.ToFirstLetterUpper()}{name}({bodyParam.Name}:{bodyParam.Type.Name}, url: string) {{");

                        //DTO Model auto-generation - could be somewhere else?
                        if (!dtoDict.Keys.Any(k => k == bodyParam.Type.Name))
                        {
                            dtoDict[bodyParam.Type.Name] = GenerateDto(bodyParam.Type);
                            if (!neededDtos.Any(d => d == bodyParam.Type.Name))
                                neededDtos.Add(bodyParam.Type.Name);
                        }
                    }
                    else if (Delete)
                        actions.AppendLine($"export function action{act.HttpMethod.ToFirstLetterUpper()}{name}(itemId: number, url: string) {{");
                    else
                        actions.AppendLine($"export function action{act.HttpMethod.ToFirstLetterUpper()}{name}(url: string) {{");
                    actions.AppendLine( "   return (dispatch: Dispatch) => {");
                    if (PostPut)
                        actions.AppendLine($"      dispatch({beginInterface.Substring(1)}({bodyParam.Name}.id));"); //Oletus: kaikilla on id
                    else if (Delete)
                        actions.AppendLine($"      dispatch({beginInterface.Substring(1)}(itemId));");
                    else
                        actions.AppendLine($"      dispatch({beginInterface.Substring(1)}());");

                    if (method.ToUpper() == "POST" || method.ToUpper() == "PUT")
                    {
                        actions.AppendLine( "      var options = {");
                        actions.AppendLine($"         method: '{method.ToUpper()}',");
                        actions.AppendLine($"         body: JSON.stringify({bodyParam.Name}),");
                        actions.AppendLine( "         headers: {");
                        actions.AppendLine( "            'Content-Type': 'application/json'");
                        actions.AppendLine( "         }");
                        actions.AppendLine( "      }");
                        actions.AppendLine("      return fetchWithAuth(url, options).then(");
                    }
                    else if (Delete)
                    {
                        actions.AppendLine("      var options = {");
                        actions.AppendLine($"         method: 'DELETE',");
                        actions.AppendLine("         headers: {");
                        actions.AppendLine("            'Content-Type': 'application/json'");
                        actions.AppendLine("         }");
                        actions.AppendLine("      }");
                        actions.AppendLine("      return fetchWithAuth(url, options).then(");
                    }
                    else
                        actions.AppendLine( "      return fetchWithAuth(url, {}).then(");
                    actions.AppendLine( "         response => {");
                    actions.AppendLine( "            //Using text() on the response so in case of json parse error, we can return the erroneus response.");
                    actions.AppendLine( "            return response.text().then((text: any) => {");
                    actions.AppendLine( "               try {");
                    actions.AppendLine( "                  let json = JSON.parse(text);");
                    actions.AppendLine($"                  console.log(\"action{act.HttpMethod.ToFirstLetterUpper()}{name}: saatu json:\");");
                    actions.AppendLine( "                  console.log(json);");
                    actions.AppendLine( "                  if (!response.ok) {");
                    actions.AppendLine($"                     return dispatch({errorInterface.Substring(1)}(response.status+\" \"+response.statusText+\" \"+json.message));");
                    actions.AppendLine( "                  }");
                    if (respType.IsGenericType && (respType.GetGenericTypeDefinition() == typeof(List<>)))
                        actions.AppendLine(GenerateListAssign(respType.GetGenericArguments().Single().Name, 18));
                    else
                        actions.AppendLine($"                  var result = Object.assign(new {respTypeName}, json) as {respTypeName};");
                    actions.AppendLine($"                  return dispatch({successInterface.Substring(1)}(result));");
                    actions.AppendLine( "               } catch (error) {");
                    actions.AppendLine($"                  console.log(\"JSON parse error: \" +error.message +\": \"+text);");
                    actions.AppendLine($"                  return dispatch({errorInterface.Substring(1)}(error.message));");
                    actions.AppendLine( "               }");
                    actions.AppendLine( "            });");
                    actions.AppendLine( "         },");
                    actions.AppendLine( "         err => {");
                    actions.AppendLine($"             console.log('action{act.HttpMethod.ToFirstLetterUpper()}{name} error: '+err);");
                    actions.AppendLine($"             return dispatch({errorInterface.Substring(1)}(err.message));");
                    actions.AppendLine( "         }");
                    actions.AppendLine( "      );");
                    actions.AppendLine( "   }");
                    actions.AppendLine( "}");
                }

                interfaces.AppendLine($"export type {grp.GroupName}Action = {combinedInterface};");

                foreach (var dto in neededDtos)
                {
                    headers.AppendLine($"import {{ {dto} }} from '../dtos/dtos-sdk';");
                }
                headers.AppendLine();

                headersDict.Add(grp.GroupName, headers);
                constantsDict.Add(grp.GroupName, constants);
                interfacesDict.Add(grp.GroupName, interfaces);
                actionCreatorsDict.Add(grp.GroupName, actionCreators);
                actionsDict.Add(grp.GroupName, actions);
                groups.Add(grp.GroupName);
                //neededDtosDict.Add(grp.GroupName, neededDtos);
            }

            foreach (var group in groups)
            {
                var constants = constantsDict[group];
                var interfaces = interfacesDict[group];
                var actionCreators = actionCreatorsDict[group];
                var actions = actionsDict[group];
                var headers = headersDict[group];
                /*var imports = new StringBuilder();
                foreach (var dto in neededDtosDict[group])
                {
                    imports.AppendLine($"import {{ {dto} }} from '../dtos/dtos-sdk';");
                }
                imports.AppendLine();*/

                var allText = headers.AppendLine().Append(constants).AppendLine().Append(interfaces).AppendLine().Append(actionCreators).Append(actions);
                File.WriteAllText($"..\\..\\finance-manager-frontend\\src\\sdk\\{group.ToLower()}\\{group}-sdk.ts", allText.ToString());
            }

            var allDtos = "";
            foreach (var dto in dtoDict)
            {
                allDtos += dto.Value;
                allDtos += "\r\n";
            }
            File.WriteAllText($"..\\..\\finance-manager-frontend\\src\\sdk\\dtos\\dtos-sdk.ts", allDtos.ToString());
        }

        private string GenerateListAssign(string type, int indent=0)
        {
            string res = "";
            string indentStr = new StringBuilder().Append(' ', indent).ToString();
            res += $"{indentStr}var result: {type}[] = [];\r\n";
            res += $"{indentStr}json.forEach((element:any) => {{\r\n";
            res += $"{indentStr}   var item = Object.assign(new {type}(), element);\r\n";
            res += $"{indentStr}   result.push(item)\r\n";
            res += $"{indentStr}}});\r\n";

            return res;
        }

        private string GenerateDto(Type type)
        {
            var allProperties = type.GetProperties();
            var inits = new List<string>();

            var dto = new StringBuilder();
            dto.AppendLine($"export class {type.Name} {{");
            foreach (var property in allProperties)
            {
                var tsName = CSharpTypeToTypescriptType(property.PropertyType);
                if (property.PropertyType.IsGenericType && (property.PropertyType.GetGenericTypeDefinition() == typeof(List<>)))
                {
                    inits.Add($"      this.{property.Name.ToCamelCase()} = [];");
                }
                dto.AppendLine($"   {property.Name.ToCamelCase()}: {tsName};");
            }
            if (inits.Count > 0)
            {
                dto.AppendLine("   constructor() {");
                foreach (var init in inits)
                {
                    dto.AppendLine(init);
                }   
                dto.AppendLine("   }");
            }
            dto.AppendLine("}");

            return dto.ToString();
        }

        private string CSharpTypeToTypescriptType(Type propType)
        {
            var type = Nullable.GetUnderlyingType(propType);
            if (type == null) //wasnt nullable
                type = propType;

            if (type == typeof(int))
                return "number";
            if (type == typeof(byte))
                return "number";
            if (type == typeof(Int64))
                return "number";
            if (type == typeof(byte[]))
                return "string";
            if (type == typeof(string))
                return "string";
            if (type == typeof(double))
                return "number";
            if (type == typeof(bool))
                return "boolean";
            if (type == typeof(DateTime))
                return "string";
            if (type.BaseType == typeof(Enum))
                return "string";

            if (!type.IsGenericType)
                return type.Name;
            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>)))
                return $"{type.GetGenericArguments().Single().Name}[]";

            throw new ArgumentException("Unrecognised type to convert");
        }

        private bool NeedsInit(Type type)
        {
            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>)))
                return true;
            return false;
        }
    }
}
