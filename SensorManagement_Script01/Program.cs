using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Npgsql;
using System.Data;
using System.Text;

namespace SensorManagement_Script01 {
    class Program {
        static void Main(string[] args) {

            SensorConditionEntities db = new SensorConditionEntities();

            StringBuilder sb = new StringBuilder();
            sb.Append("Server=ubi-t01.naist.jp;");
            sb.Append("Port=5432;");
            sb.Append("User Id=ubishop;");
            sb.Append("Password=3bnN1HNR;");
            sb.Append("Database=smarthouse");

            string connString = sb.ToString();

            StringBuilder sb2 = new StringBuilder();
            sb2.Append("Server=ubi-t02.naist.jp;");
            sb2.Append("Port=5432;");
            sb2.Append("User Id=postgres;");
            sb2.Append("Password=pgsql123;");
            sb2.Append("Database=dragondb");

            string connString2 = sb2.ToString();

            List<string> sensor_types = new List<string>();
            List<string> sensor_ids = new List<string>();

            sensor_types.Add("1");
            sensor_types.Add("2");
            sensor_types.Add("3");
            sensor_types.Add("4");
            sensor_types.Add("7");
            sensor_types.Add("14");

            while (true) {

                DateTime now = System.DateTime.Now;
                string now1 = now.AddDays(-1).ToString().Split(' ')[0];
                //DateTime now = new DateTime(2015,10,18,0,0,0);
                Console.WriteLine("NowDate: " + now.ToString());

                foreach (string sensor_type in sensor_types) {

                    sensor_ids.Clear();
                    if (sensor_type.Equals("1") || sensor_type.Equals("2") || sensor_type.Equals("3") || sensor_type.Equals("4")) {
                        foreach (var row in db.EnvironmentSensors) {
                            sensor_ids.Add(row.SensorID.ToString());
                        }
                    } else if (sensor_type.Equals("7")) {
                        foreach (var row in db.DoorSensors) {
                            sensor_ids.Add(row.SensorID.ToString());
                        }
                    } else if (sensor_type.Equals("14")) {
                        foreach (var row in db.PowerSensors) {
                            sensor_ids.Add(row.SensorID.ToString());
                        }
                    }

                    foreach (string sensor_id in sensor_ids) {
                        using (var con = new NpgsqlConnection(connString)) {
                            con.Open();

                            /*
                            sensor_type = 1:温度
                             *            2:湿度
                             *            3:照度
                             *            4:人感
                             *            7:開閉
                             *            14:電力
                            */

                            //データ検索
                            var cmd = new NpgsqlCommand(@"select datetime from sensor_values where ((sensor_type=" + sensor_type + ") and (sensor=" + sensor_id + ")) and (datetime > '"+now1+"') order by pkey desc limit 1;", con);
                            Console.WriteLine("sensor_type: " + sensor_type + " , sensorID: " + sensor_id);
                            try {
                                var dataReader = cmd.ExecuteReader();
                                while (dataReader.Read()) {
                                    Console.WriteLine("datetime: " + ((DateTime)dataReader["datetime"]).ToString());
                                    int sensorID = int.Parse(sensor_id);
                                    if (sensor_type.Equals("1")) {
                                        db.EnvironmentSensors.Where(p => p.SensorID == sensorID).SingleOrDefault().LatestUploadDate_Temp = ((DateTime)dataReader["datetime"]).ToString();
                                    } else if (sensor_type.Equals("2")) {
                                        db.EnvironmentSensors.Where(p => p.SensorID == sensorID).SingleOrDefault().LatestUploadDate_Humi = ((DateTime)dataReader["datetime"]).ToString();
                                    } else if (sensor_type.Equals("3")) {
                                        if (Math.Abs((now - (DateTime)dataReader["datetime"]).TotalMinutes) > 60) {
                                            SendMail("環境センサ（ID:" + sensor_id + "）電池切れの可能性有り", "環境センサ（ID:" + sensor_id + "）の照度センサ値が60分間以上送信されていません。" + Environment.NewLine + "電池切れの可能性がありますので、確認してください。", "moriya.kazuki.228@gmail.com");
                                            SendMail("環境センサ（ID:" + sensor_id + "）電池切れの可能性有り", "環境センサ（ID:" + sensor_id + "）の照度センサ値が60分間以上送信されていません。" + Environment.NewLine + "電池切れの可能性がありますので、確認してください。", "nakagawa.eri.nz6@is.naist.jp");
                                        }
                                        db.EnvironmentSensors.Where(p => p.SensorID == sensorID).SingleOrDefault().LatestUploadDate_Light = ((DateTime)dataReader["datetime"]).ToString();
                                    } else if (sensor_type.Equals("4")) {
                                        db.EnvironmentSensors.Where(p => p.SensorID == sensorID).SingleOrDefault().LatestUploadDate_Human = ((DateTime)dataReader["datetime"]).ToString();
                                    } else if (sensor_type.Equals("7")) {
                                        db.DoorSensors.Where(p => p.SensorID == sensorID).SingleOrDefault().LatestUploadDate = ((DateTime)dataReader["datetime"]).ToString();
                                    } else if (sensor_type.Equals("14")) {
                                        db.PowerSensors.Where(p => p.SensorID == sensorID).SingleOrDefault().LatestUploadDate = ((DateTime)dataReader["datetime"]).ToString();
                                    }
                                }
                            } catch {
                                Console.WriteLine("スルー");
                            }

                            con.Close();

                        }
                    }

                }


                for (int i = 1; i <= 4; i++) {
                    using (var con = new NpgsqlConnection(connString2)) {
                        con.Open();

                        /*
                        sensor_type = 1:温度
                         *            2:湿度
                         *            3:照度
                         *            4:人感
                         *            7:開閉
                         *            14:電力
                        */

                        //データ検索
                        var cmd = new NpgsqlCommand(@"select time_stamp from tag_position where (tag_id=" + i.ToString() + ") order by time_stamp desc limit 1;", con);
                        Console.WriteLine("tag_id: " + i.ToString());
                        try {
                            var dataReader = cmd.ExecuteReader();
                            while (dataReader.Read()) {
                                Console.WriteLine("time_stamp: " + ((DateTime)dataReader["time_stamp"]).ToString());
                                db.PositionSensors.Where(p => p.SensorID == i).FirstOrDefault().LatestUploadDate = ((DateTime)dataReader["time_stamp"]).ToString();
                            }
                        } catch {
                            Console.WriteLine("スルー");
                        }

                        con.Close();

                    }
                }
                db.SaveChanges();
                Console.WriteLine("End");
                System.Threading.Thread.Sleep(24 * 60 * 60 * 1000);
            }

        }
        static void SendMail(string subject, string text, string destination) {
            Console.WriteLine("SendMail_Start");

            System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage(
               "cats.marimocode.mail@gmail.com", destination,
               subject, text);
            System.Net.Mail.SmtpClient sc = new System.Net.Mail.SmtpClient();
            sc.Host = "smtp.gmail.com";
            sc.Port = 587;
            sc.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
            sc.Credentials = new System.Net.NetworkCredential("cats.marimocode.mail@gmail.com", "MarimoCats");
            sc.EnableSsl = true;
            sc.Send(msg);
            msg.Dispose();
            sc.Dispose();

            Console.WriteLine("SendMail_End");
        }
    }
}
