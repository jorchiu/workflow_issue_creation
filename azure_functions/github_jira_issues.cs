#r "Newtonsoft.Json"

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    log.LogInformation("C# HTTP trigger function processed a request.");
    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    dynamic data = JsonConvert.DeserializeObject(requestBody);
    string jira_issue = data?.issue.fields.summary;
    HttpClient client = new HttpClient();

    var secretValue = Environment.GetEnvironmentVariable("TOKEN", EnvironmentVariableTarget.Process);  
    
    client.DefaultRequestHeaders.Add("Authorization", secretValue);
    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
    client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    client.DefaultRequestHeaders.Add("User-Agent", "AzureFns");

    string uri = "https://api.github.com/repos/{GITHUB_USERNAME}/{REPO_NAME}/dispatches";
    DateTime pacific = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Pacific Standard Time");
    string title = "Jira Issue: " + jira_issue + " " + pacific.ToString();

    var dispatch = new Dispatch(title, jira_issue);

    var stringContent = new StringContent(JsonConvert.SerializeObject(dispatch), Encoding.UTF8, "application/json");
    HttpResponseMessage response = client.PostAsync(uri, stringContent).Result;
    string responseBody = await response.Content.ReadAsStringAsync();
    log.LogInformation(responseBody);
    string responseMessage = string.IsNullOrEmpty(jira_issue)
        ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {jira_issue}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
}

public class Dispatch {
    public string event_type{get; set;}
    public Issue client_payload{get; set;}

    public Dispatch(string issueTitle, string issueBody) {
        event_type = "issue-creation";        
        client_payload = new Issue();
        client_payload.labels = "Jira";
        client_payload.title = issueTitle;
        client_payload.body = issueBody;
    }
}

public class Issue {
    public string title{get; set;}
    public string body{get; set;}
    public string labels{get; set;}
}