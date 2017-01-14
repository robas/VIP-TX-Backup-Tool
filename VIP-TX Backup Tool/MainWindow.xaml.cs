using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VIP_TX_Backup_Tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //int progressBarStep = 0;
        public MainWindow()
        {
            InitializeComponent();
            backupPathTextBox.Text = System.IO.Directory.GetCurrentDirectory();
        }

        /*
         * Handles the Backup Button click
         * 1. Generates the URL for each IP provided
         * 2. Gets the backup file for each encoder
         * 3. Save the backup as a system file
         */
        private void bkpButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> validIpList = getValidIpList();
            outputTextBlock.Text = "";
            //progressBar.Value = 0;
            //progressBarStep = 100/validIpList.Count;
            outputTextBlock.Inlines.Add("Processando encoders...\n");
            
            foreach (string ip in validIpList)
            {
                //Thread encoderThread = new Thread(() => backupEncoder(ip));
                //encoderThread.Start();
               backupEncoder(ip, usernameTextBox.Text, passwordBox.Password);
            }   
        }

        /*
         * Requests the encoder backup and saves it to a file
         */
        private void backupEncoder(string encoderIp, string username, string password)
        {
            outputTextBlock.Inlines.Add(encoderIp + ": ");
            string url = "http://" + encoderIp + "/backup/backup.cgi";
            string fileName = backupPathTextBox.Text + "\\" + encoderIp + ".tar";
            getVipTxBackup2(url, username, password, fileName);
            /*if (backupContent.Length != 0)
            {
                //string fileName = backupPathTextBox.Text+"\\"+encoderIp + ".tar";
                Console.WriteLine(fileName);
                saveFile(fileName, backupContent);
            }*/
            //progressBar.Value += progressBarStep;
        }

        /*
         * Checks all valid IP addresses provided on the ipListTextBox
         * Returns a Dictionary<IP address, URL address>
         */
        private List<string> getValidIpList()
        {
            List<string> ipList = new List<string>();
            outputTextBlock.Inlines.Add("Lendo lista de IPs...\n");
            using (StringReader reader = new StringReader(ipListTextBox.Text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line == "")
                    {
                        continue;
                    }
                    else
                    {
                        // Not empty IP list. Checking for valid IP addresses
                        System.Text.RegularExpressions.Regex ipRegex = new System.Text.RegularExpressions.Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
                        if (ipRegex.IsMatch(line))
                        {
                            // Valid IP
                            ipList.Add(line);
                            outputTextBlock.Inlines.Add(line + ": Válido\n");
                        }
                        else
                        {
                            outputTextBlock.Inlines.Add(line + ": inválido\n");
                        }
                    }
                }
                return ipList;
            }
        }

        /*
         * Makes the http request to receive the backup
         */
        private void getVipTxBackup2(string url, string username, string password, string fileName)
        {
            using (var webClient = new WebClient())
            {
                string authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(username+":"+password));
                webClient.Headers[HttpRequestHeader.Authorization] = "Basic " + authInfo;
                webClient.Headers[HttpRequestHeader.ContentType] = "application/octet-stream";
                webClient.Headers[HttpRequestHeader.UserAgent] = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";
                Uri _url = new Uri(url);
                webClient.DownloadFileAsync(_url, fileName);
                //return data;
            }
        }

        /*
         * Save the backup as a system file
         */
        private bool saveFile(string fileName, string fileContent)
        {
            string path = fileName;
            bool result = true;

            if (File.Exists(path))
            {
                File.Delete(path);
            }
            try
            {
                File.WriteAllText(path, fileContent);
                outputTextBlock.Inlines.Add("Backup salvo em " + path + "\n");
            }
            catch (IOException e)
            {
                outputTextBlock.Inlines.Add("Erro ao salvar arquivo" + path + ". Mensagem: " + e.Message + "\n");
                //Console.WriteLine("Error writing to {0}. Message = {1}", path, e.Message);
                result = false;
            }
            finally
            {
                    
            }
            
            return result;
        }

        private void ptzBkpButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> validIpList = getValidIpList();
            outputTextBlock.Text = "";

            outputTextBlock.Inlines.Add("Processando encoders...\n");

            foreach (string ip in validIpList)
            {
                //Thread encoderThread = new Thread(() => backupEncoder(ip));
                //encoderThread.Start();
                backupPTZ(ip);
            }
        }

        /*
         * Example: Data-1.Ptz-1.Presets=[],[0,95,216330,10980,2976000,][3,1,20530,12300,2230000,][3,2,218800,13100,2230000,]
         * 
         * Example: Data-1.Ptz-1.Presets=[],
         * 
         */
        private void backupPTZ(string ip)
        {
            string url = "http://" + ip + "/params/get.cgi?Data-1.Ptz-1.Presets";
            string presets = vipTxRequest(url, usernameTextBox.Text, passwordBox.Password);
            if (presets == "[],")
            {
                outputTextBlock.Inlines.Add(ip + ": Nenhum preset configurado.");
            }
            else
            {
                outputTextBlock.Inlines.Add(ip + ": " + presets);
            }
        }

        private string vipTxRequest(string url, string username, string password)
        {
            string responseContent = "";

            Uri myUri = new Uri(url);
            WebRequest myWebRequest = HttpWebRequest.Create(myUri);
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)myWebRequest;

            NetworkCredential myNetworkCredential = new NetworkCredential(username, password);
            CredentialCache myCredentialCache = new CredentialCache();
            myCredentialCache.Add(myUri, "Basic", myNetworkCredential);

            myHttpWebRequest.PreAuthenticate = true;
            myHttpWebRequest.Credentials = myCredentialCache;
            myHttpWebRequest.Timeout = 5000;

            try
            {
                WebResponse myWebResponse = myHttpWebRequest.GetResponse();
                Stream responseStream = myWebResponse.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(responseStream, Encoding.Default);

                responseContent = myStreamReader.ReadToEnd();
            }
            catch (System.Net.WebException e)
            {
                outputTextBlock.Inlines.Add("Erro ao comunicar com o Encoder. Mensagem: " + e.Message + "\n");
            }
            return responseContent;

        }
    }
}
