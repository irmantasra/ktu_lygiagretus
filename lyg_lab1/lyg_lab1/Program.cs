using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace lyg_lab1
{
    public class Trip
    {
        public string car { get; set; }
        public double origin_lat { get; set; }
        public double origin_long { get; set; }
        public double destination_lat { get; set; }
        public double destination_long { get; set; }
        public double fuel_consumption { get; set; }
        public int passengers_num { get; set; }

        private const string URL = "https://api.openrouteservice.org/v2/directions/driving-car";

        public Trip(string car, double origin_lat, double origin_long, double destination_lat, double destination_long, double fuel_consumption, int passengers_num)
        {
            this.car = car;
            this.origin_lat = origin_lat;
            this.origin_long = origin_long;
            this.destination_lat = destination_lat;
            this.destination_long = destination_long;
            this.fuel_consumption = fuel_consumption;
            this.passengers_num = passengers_num;
        }

        public void getRoute()
        {
            var client = new RestClient("https://api.openrouteservice.org/v2/directions/driving-car");
            var request = new RestRequest();
            request.AddHeader("Authorization", "5b3ce3597851110001cf62480c7195c7cd2e4e13b15c3844a4332e62");
            request.AddHeader("content-type", "application/json");
            var temp = new requestBody(new double[,] { { this.origin_lat, this.origin_long }, { this.destination_lat, this.destination_long } });
            Console.WriteLine(JsonConvert.SerializeObject(temp));
            request.AddJsonBody(JsonConvert.SerializeObject(temp));
            var response = client.Post(request);
            var content = response.Content;
            Console.WriteLine(content);
        }
    }

    class requestBody
    {
        public double[,] coordinates { get; set; }

        public requestBody(double[,] coordinates)
        {
            this.coordinates = coordinates;
        }
    }
    
    class Program
    {
        static void Main(string[] args)
        {
            Trip tripas = new Trip("Honda", 55.3697037, 25.549938, 54.5394379, 21.3592464, 9.8, 3);
            tripas.getRoute();
            Console.ReadKey();
        }
    }
}
