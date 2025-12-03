// <copyright file="ValuesController.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace TestApplication.AspNet.NetFramework.Controllers;

public class ValuesController : ApiController
{
    // GET api/values
    public HttpResponseMessage Get()
    {
        var response = Request.CreateResponse(HttpStatusCode.OK, new[] { "value1", "value2" });
        response.Headers.Add("Custom-Response-Test-Header1", "Test-Value1");
        response.Headers.Add("Custom-Response-Test-Header2", "Test-Value2");
        response.Headers.Add("Custom-Response-Test-Header3", "Test-Value3");
        return response;
    }

    // GET api/values/5
    public string Get(int id)
    {
        return "value";
    }

    // POST api/values
    public void Post([FromBody] string value)
    {
    }

    // PUT api/values/5
    public void Put(int id, [FromBody] string value)
    {
    }

    // DELETE api/values/5
    public void Delete(int id)
    {
    }
}
