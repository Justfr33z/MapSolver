using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MapSolver.Utils {
    public class AntiCaptcha {
        private readonly HttpClient httpClient;
        private readonly string clientKey;

        private const string CREATE_TASK = "https://api.anti-captcha.com/createTask";
        private const string TASK_RESULT = "https://api.anti-captcha.com/getTaskResult";
        private const string GET_BALANCE = "https://api.anti-captcha.com/getBalance ";

        public AntiCaptcha(string clientKey) {
            httpClient = new HttpClient();
            this.clientKey = clientKey;
        }

        public async Task<float> GetBalanceAsync() {
            var json =
                new JObject(
                    new JProperty("clientKey", clientKey));

            var responseString = await httpClient.PostAsync(GET_BALANCE, new StringContent(json.ToString(), Encoding.UTF8, "application/json"))
                .Result
                .Content
                .ReadAsStringAsync();

            var responseJson = JObject.Parse(responseString);

            var errorId = Convert.ToInt32(responseJson["errorId"]);
            if (errorId != 0)
                throw new Exception(responseJson["errorDescription"].ToString());

            return Convert.ToSingle(responseJson["balance"]);
        }

        public async Task<int> CreateTaskAsync(string encodedImage) {
            var taskJson =
                new JObject(
                    new JProperty("clientKey", clientKey),
                        new JProperty("task",
                            new JObject(
                                new JProperty("type", "ImageToTextTask"),
                                new JProperty("body", encodedImage))));

            var responseString = await httpClient.PostAsync(CREATE_TASK, new StringContent(taskJson.ToString(), Encoding.UTF8, "application/json"))
            .Result
            .Content
            .ReadAsStringAsync();

            var responseJson = JObject.Parse(responseString);

            var errorId = Convert.ToInt32(responseJson["errorId"]);
            if (errorId != 0)
                throw new Exception(responseJson["errorDescription"].ToString());

            return Convert.ToInt32(responseJson["taskId"]);
        }

        public async Task<String> GetTaskResultAsync(int taskId) {
            var json =
                new JObject(
                    new JProperty("clientKey", clientKey),
                    new JProperty("taskId", taskId));

            while (true) {
                var responseString = await httpClient.PostAsync(TASK_RESULT, new StringContent(json.ToString(), Encoding.UTF8, "application/json"))
                .Result
                .Content
                .ReadAsStringAsync();

                var responseJson = JObject.Parse(responseString);

                var errorId = Convert.ToInt32(responseJson["errorId"]);
                if (errorId != 0)
                    throw new Exception(responseJson["errorDescription"].ToString());

                if (responseJson["status"].ToString() == "ready")
                    return responseJson["solution"]["text"].ToString();

                await Task.Delay(1000);
            }
        }
    }
}
