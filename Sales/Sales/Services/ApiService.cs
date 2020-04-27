namespace Sales.Services
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Common.Models;
    using Newtonsoft.Json;
    using Plugin.Connectivity;
    using Sales.Helpers;

    public class ApiService
    {
        public async Task<Response> CheckConnection()
        {
            if (!CrossConnectivity.Current.IsConnected)
            {
                return new Response
                {
                    IsSuccess = false,
                    //Message = Languages.TurnOnInternet,
                    Message = Languages.TurnOnInternet,
                };
            }

            var isReachable = await CrossConnectivity.Current.IsRemoteReachable("google.com");
            if (!isReachable)
            {
                return new Response
                {
                    IsSuccess = false,
                    //Message = Languages.NoInternet,
                    Message = Languages.NoInternet,
                };
            }

            return new Response
            {
                IsSuccess = true,
            };
        }



        //Método genérico que consume cualquier controlador API
        public async Task<Response> GetList<T>(string urlBase, string prefix, string controller)
        {
            try
            {
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (requestMessage, certificate, chain, policyErrors) => true;

                var client = new HttpClient(handler);
                //var client = new HttpClient();                
                client.BaseAddress = new Uri(urlBase);
                var url = $"{prefix}{controller}"; //Concatenar string
                var response = await client.GetAsync(url);
                var answer = await response.Content.ReadAsStringAsync();    //json en string

                if (!response.IsSuccessStatusCode)    //FALLA
                {
                    return new Response
                    {
                        IsSuccess = false,
                        Message = answer,
                    };
                }

                //Deserializamos el JSON a objeto 
                var list = JsonConvert.DeserializeObject<List<T>>(answer);
                return new Response
                {
                    IsSuccess = true,
                    Result = list,
                };
            }
            catch (Exception ex)
            {

                return new Response
                {
                    IsSuccess = false,
                    Message = ex.Message,                    
                };
            }
        }

        public async Task<Response> Post<T>(string urlBase, string prefix, string controller, T model)
        {
            try
            {
                var request = JsonConvert.SerializeObject(model);
                var content = new StringContent(request, Encoding.UTF8, "application/json");
                
                //var client = new HttpClient();
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (requestMessage, certificate, chain, policyErrors) => true;
                var client = new HttpClient(handler);

                client.BaseAddress = new Uri(urlBase);
                var url = $"{prefix}{controller}";
                var response = await client.PostAsync(url, content);
                var answer = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new Response
                    {
                        IsSuccess = false,
                        Message = answer,
                    };
                }

                var obj = JsonConvert.DeserializeObject<T>(answer);

                return new Response
                {
                    IsSuccess = true,
                    Result = obj,
                };
            }
            catch (Exception ex)
            {
                return new Response
                {
                    IsSuccess = false,
                    Message = ex.Message,
                };
            }
        }

    }
}
