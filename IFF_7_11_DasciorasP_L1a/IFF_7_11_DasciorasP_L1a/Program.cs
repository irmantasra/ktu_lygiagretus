using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;

namespace IFF_7_11_DasciorasP_L1a
{
    class Program
    {
        static class ProgramData
        {
            public static Picture[] initialData;  //parsed json data
            public static int numOfThreads = 4;   //The number of threads
            public static List<Thread> Threads;   //Thread List
            public static int numberOfDataProduced = 0; // counter to track how many objects has been moved to monitor
            public static int numberOfDataConsumed = 0; // counter to track how many objects has been moved out from monitor        
            public static DataArray dataArray;
            public static ResultArray resultArray;
            public const string picturesPath = @"C:\Users\Paulius\Desktop\Trečias kursas\Lygiagretusis programavimas\IFF_7_11_DasciorasP_L1a\Downloaded pictures"; //path to folder with downloaded JsonData
        }

        public class Picture
        {
            public string fotoUrl { get; set; }
            public int height { get; set; }
            public int width { get; set; }
            public double pictureSize { get; set; }
        }

        private class DataArray
        {
            private Picture[] pictures;
            public int count;
            private readonly int MIN;
            private readonly int MAX;
            private readonly object _locker;

            public DataArray(int size)
            {
                pictures = new Picture[size];
                count = 0;
                MIN = 0;
                MAX = size;
                _locker = new object();
            }

            public bool HasWorkFinished()
            {
                lock (_locker)
                {
                    return pictures[0] != null && pictures[0].height == -1;
                }
            }

            public void Add(Picture picture)
            {
                lock (_locker)
                {
                    while (MAX == count)
                    {
                        Monitor.Wait(_locker);
                    }

                    pictures[count++] = picture;
                    ProgramData.numberOfDataProduced++;
                    Monitor.PulseAll(_locker);
                }
            }

            public Picture Remove()
            {
                Picture returnValue;
                lock (_locker)
                {
                    while (MIN == count && !HasWorkFinished())
                    {
                        Monitor.Wait(_locker);
                    }

                    if (HasWorkFinished()) return null;

                    returnValue = pictures[0];
                    RemoveFirst();
                    ProgramData.numberOfDataConsumed++;
                    Monitor.PulseAll(_locker);
                }
                return returnValue;
            }
            private void RemoveFirst()
            {
                Picture[] newArray = new Picture[MAX];
                for (int i = 1; i < count; i++)
                {
                    newArray[i - 1] = pictures[i];
                }
                pictures = newArray;
                count--;
            }
        }

        public class ResultArray
        {
            public Picture[] pictures { get; }

            public int Count { get; set; }

            private readonly object _locker;
            public ResultArray(int size)
            {
                Count = 0;
                pictures = new Picture[size];
                _locker = new object();
            }
            public void Add(Picture picture)
            {
                lock (_locker)
                {
                    picture.pictureSize = CalculateSize(DownloadImageFromUrl(picture.fotoUrl + picture.width.ToString()));
                    if (Count == 0)
                    {
                        pictures[Count++] = picture;
                    }
                    else if (picture.pictureSize >= pictures[Count - 1].pictureSize)
                    {
                        pictures[Count++] = picture;
                    }
                    else if (picture.pictureSize <= pictures[0].pictureSize)
                    {

                        for (int i = Count; i > 0; i--)
                        {

                            pictures[i] = pictures[i - 1];

                        }
                        pictures[0] = picture;
                        Count++;
                    }
                    else
                    {
                        for (int i = 0; i < Count; i++)
                        {
                            if (picture.pictureSize < pictures[i].pictureSize)
                            {
                                for (int j = Count; j > i; j--)
                                {
                                    pictures[j] = pictures[j - 1];
                                }
                                pictures[i] = picture;
                                Count++;
                                break;
                            }
                        }
                    }
                }
            }

            static void Main(string[] args)
            {
                string dataFile = "C:/Users/Paulius/Desktop/Trečias kursas/Lygiagretusis programavimas/Iff7-11_DasciorasP_L1_dat_1.json";
                ProgramData.initialData = ReadJson(dataFile);

                ProgramData.dataArray = new DataArray(ProgramData.initialData.Length/2);
                ProgramData.resultArray = new ResultArray(ProgramData.initialData.Length);
                CreateThreads();
                StartThreads();
                MoveDataToMonitor();
                JoinThreads();

                for (int i = 0; i < ProgramData.resultArray.Count; i++)
                {
                    Console.WriteLine(ProgramData.resultArray.pictures[i].pictureSize);
                }

                //Console.WriteLine( ProgramData.ResultArray.pictures[0] ); 
            }

            public static void CreateThreads()
            {
                ProgramData.Threads = Enumerable.Range(0, ProgramData.numOfThreads).Select(i => new Thread(DeleteElement)).ToList();
            }

            public static void StartThreads()
            {
                foreach (var thread in ProgramData.Threads)
                {
                    thread.Start();
                }
            }

            public static void JoinThreads()
            {
                foreach (var thread in ProgramData.Threads)
                {
                    thread.Join();
                }
            }

            public static void MoveDataToMonitor()
            {
                foreach (var picture in ProgramData.initialData)
                {
                    ProgramData.dataArray.Add(picture);
                }
            }

            public static void DeleteElement()
            {
                while (true)
                {
                    var picture = ProgramData.dataArray.Remove();
                    if (picture == null) break;
                    
                    if (CheckFrame(picture)) 
                    {
                        ProgramData.resultArray.Add(picture);

                        //Console.WriteLine(CalculateSize(DownloadImageFromUrl(picture.fotoUrl + picture.width.ToString())));
                    }
                }
            }

            public static bool CheckFrame(Picture picture)
            {
                if (picture.height == picture.width) return true;
                return false;
            }

            public static System.Drawing.Image DownloadImageFromUrl(string imageUrl)
            {
                System.Drawing.Image image = null;

                try
                {
                    System.Net.HttpWebRequest webRequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(imageUrl);
                    webRequest.AllowWriteStreamBuffering = true;
                    webRequest.Timeout = 30000;

                    System.Net.WebResponse webResponse = webRequest.GetResponse();

                    System.IO.Stream stream = webResponse.GetResponseStream();

                    image = System.Drawing.Image.FromStream(stream);

                    webResponse.Close();
                }
                catch (Exception ex)
                {
                    return null;
                }

                SavePicture(image);

                return image;
            }

            public static double CalculateSize(System.Drawing.Image image)
            {
                ImageConverter _imageConverter = new ImageConverter();
                byte[] xByte = (byte[])_imageConverter.ConvertTo(image, typeof(byte[]));

                double sizeMb = (xByte.Length / 1024f);

                return sizeMb;
            }

            public static void SavePicture(System.Drawing.Image image)
            {
                String imgName = image.GetHashCode().ToString() + ".png";
                string fileName = System.IO.Path.Combine(ProgramData.picturesPath, imgName);
                image.Save(fileName);
            }

            private static Picture[] ReadJson(string filePath)
            {
                using (StreamReader r = new StreamReader(filePath))
                {
                    string json = r.ReadToEnd();
                    Picture[] data = JsonConvert.DeserializeObject<Picture[]>(json);
                    Picture[] pictures = new Picture[data.Length + 1];
                    for (int i = 0; i < data.Length; i++)
                    {
                        pictures[i] = data[i];
                    }

                    // dummy object
                    pictures[pictures.Length - 1] = new Picture { height = -1 };
                    
                    return pictures;
                }
            }
        }
    }
}
