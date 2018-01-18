using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConecttaToGo.Model;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace ConecttaToGo
{
    class Program
    {
        static void Main(string[] args)
        {
            var chips = new List<Chip>();

            var chip = new Chip();

            chip = new Chip();
            chip.Numero = "71996447058";
            chip.SenhaToGo = "#oem2650";
            chips.Add(chip);

            //chip = new Chip();
            //chip.Numero = "71996757685";
            //chip.SenhaToGo = "ms6tgs1";
            //chips.Add(chip);

            //chip = new Chip();
            //chip.Numero = "71996148031";
            //chip.SenhaToGo = "ms6tgs2";
            //chips.Add(chip);

            //chip = new Chip();
            //chip.Numero = "71999870248";
            //chip.SenhaToGo = "ms6tgs3";
            //chips.Add(chip);

            //chip = new Chip();
            //chip.Numero = "71999627312";
            //chip.SenhaToGo = "ms6tgs4";
            //chips.Add(chip);

            //chip = new Chip();
            //chip.Numero = "71999116980";
            //chip.SenhaToGo = "ms6tgs5";
            //chips.Add(chip);


            foreach (var c in chips)
            {
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("https://api-br.tugoapp.com/users/login?remember_me=false");

                httpWebRequest.Method = "POST";
                httpWebRequest.Accept = "application/json, text/plain, */*";
                httpWebRequest.ContentType = "application/json; charset=UTF-8";

                httpWebRequest.Headers.Add(HttpRequestHeader.Authorization, c.AuthorizationToGO);
                httpWebRequest.Headers.Add("X-User-Agent", "GConnect/17.8.54 (WD; Chrome; 61.0.3163.100; Windows; 10;)(6bbb038c-3cbc-47ba-fc21-17ab8229e4af)");


                using (Stream s = httpWebRequest.GetRequestStream())
                {
                    int i = 0;
                }

                using (HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    using (StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
                    {
                        string retorno = streamReader.ReadToEnd();

                        JObject json = JObject.Parse(retorno);

                        c.SenhaWs = json["config"]["sip"]["password"].ToString();
                    }
                }

                Task.Factory.StartNew(() => NewWsConnection(c));
            }

            Console.ReadLine();

            int o = 0;
        }

        static void NewWsConnection(Chip chip)
        {
            using (var ws = new WebSocket("wss://mapr-tga-wrgv.jajah.com/"))
            {
                Console.Write("Iniciado");

                ws.OnMessage += (sender, e) =>
                  Console.WriteLine(e.Data);

                ws.Connect();
                ws.Send("{\"method\":\"login\",\"type\":\"req\",\"username\":\""+ chip.Username +"\",\"password\":\""+ chip.SenhaWs +"\",\"domain\":\"bra.voip.gconnect.jajah.com\",\"userAgent\":\"GConnect/17.8.54 (WD; Chrome; 61.0.3163.100; Windows; 10;)(TUGOWEB-ece28b64-eb55-4000-ad6a-d0dcf6e3802c)\",\"sdkObj\":\"f6f1ef\",\"sdkVersion\":\"17.10.0.170223\",\"userCapability\":{ \"msg\":true,\"audio\":false},\"deviceId\":\"TUGOWEB-ece28b64-eb55-4000-ad6a-d0dcf6e3802c\"}");

                for (int i = 0; i < 10; i++)
                {
                    var messId = Guid.NewGuid().ToString();

                    var destino = chip.Numero;

                    var mensagem = Guid.NewGuid().ToString() +"AAAA";

                    ws.Send("{\"method\":\"msg\",\"type\":\"req\",\"to\":\"55" + destino + "\",\"from\":\"" + chip.Username + "\",\"fromDomain\":\"bra.voip.gconnect.jajah.com\",\"toDomain\":\"bra.pstn.gconnect.jajah.com\",\"callId\":\"74279e0d5f305551@199.244.69.27\",\"sdkMsgObj\":\"cca888\",\"userAgent\":\"GConnect/17.8.54 (WD; Chrome; 61.0.3163.100; Windows; 10;)(TUGOWEB-ece28b64-eb55-4000-ad6a-d0dcf6e3802c)\",\"content\":{ \"type\":\"text/plain;subset=LATIN1;charset=utf-8\",\"body\":\"" + mensagem + "\"},\"reqUri\":\"0" + destino + "@bra.pstn.gconnect.jajah.com\",\"headers\":{ \"Comm-Logging\":\"on\",\"Comm-Type\":\"text\",\"Comm-Correlation-ID\":\"TUGOWEB-ece28b64-eb55-4000-ad6a-d0dcf6e3802c.y6hhr3.MO#84:5571996447058\",\"Message-Id\":\"" + messId + "\"}}");
                }

                Console.ReadKey(true);
            }
        }
    }
}
