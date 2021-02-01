using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using System.Data;
using System.Collections.ObjectModel;

namespace WpfApp1
{
    public class IP
    {
        public string Name { get; set; }
        public string IpDecimal { get; set; }
        public string IpHex { get; set; }
        public string IpBinary { get; set; }
        
        public IP (string name, string ipDecimal, string ipHex = "", string ipBinary = "")
        {
            this.Name = name;
            this.IpDecimal = ipDecimal;
            this.IpHex = ipHex;
            this.IpBinary = ipBinary;
        }
    }


    public partial class MainWindow : Window
    {
        public ObservableCollection<IP> IPs { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void MainWindow_ComboBox(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            ComboBoxItem selectedItem = (ComboBoxItem)comboBox.SelectedItem;
        }

        private void MainWindow_Button(object sender, MouseButtonEventArgs e)
        {
            string ipAddress = textBoxIpAddress.Text;
            if (isInputCorrect(ipAddress))
            {
                // задание значений и вызов функций
                string ipHexFormat = string.Format("{0:X}", ConvertIpToHexadecimal(ipAddress));
                string ipBinaryFormat = ConvertIpToBinary(ipAddress);
                string subnetMask = ((ComboBoxItem)comboBoxMask.SelectedItem).Content.ToString().Remove(0, 6);
                string subnetMaskPrefix = GetSubnetMaskPrefix(ConvertIpToBinary(subnetMask));
                string reverseSubnetMask = GetReverseIp(subnetMask);
                string networkAddress = GetNetworkAddress(ipAddress, subnetMask);
                string broadcastAddress = GetBroadcastAddress(networkAddress, reverseSubnetMask);
                string subnetsCount = GetSubnetsCount(ConvertIpToBinary(subnetMask));
                string hostsCount = GetHostsCount(ConvertIpToBinary(subnetMask));

                // вывод результата на экран
                IPs = new ObservableCollection<IP>();
                IPs.Add(new IP("IP адрес", ipAddress, ipHexFormat, ipBinaryFormat));
                IPs.Add(new IP("Префикс маски подсети", subnetMaskPrefix));
                IPs.Add(new IP("Маска подсети", subnetMask,
                    ConvertIpToHexadecimal(subnetMask), ConvertIpToBinary(subnetMask)));
                IPs.Add(new IP("Обратная маска подсети (wildcard mask)", reverseSubnetMask,
                    ConvertIpToHexadecimal(reverseSubnetMask), ConvertIpToBinary(reverseSubnetMask)));
                IPs.Add(new IP("IP адрес сети", networkAddress, ConvertIpToHexadecimal(networkAddress),
                    ConvertIpToBinary(networkAddress)));
                IPs.Add(new IP("Широковещательный адрес", broadcastAddress,
                    ConvertIpToHexadecimal(broadcastAddress), ConvertIpToBinary(broadcastAddress)));
                IPs.Add(new IP("IP адрес первого хоста", GetFirstHost(networkAddress),
                    ConvertIpToHexadecimal(GetFirstHost(networkAddress)), ConvertIpToBinary(GetFirstHost(networkAddress))));
                IPs.Add(new IP("IP адрес последнего хоста", GetLastHost(broadcastAddress),
                    ConvertIpToHexadecimal(GetLastHost(broadcastAddress)), ConvertIpToBinary(GetLastHost(broadcastAddress))));
                IPs.Add(new IP("Количество доступных портов", subnetsCount));
                IPs.Add(new IP("Количество доступных адресов для хостов", hostsCount));

                dataGrid.ItemsSource = IPs;
            }
        }

        // non-properties functions

        public static string GetFirstHost(string networkAddress)
        {
            string[] subs = networkAddress.Split('.');
            // прибавляет единицу к крайней части ip-адреса
            int sub = Int32.Parse(subs[3]);
            if (sub < 255)
            {
                sub += 1;
                subs[3] = sub.ToString();
            }
            return String.Join(".", subs);
        }

        public static string GetLastHost(string broadcastAddress)
        {
            string[] subs = broadcastAddress.Split('.');
            // отнимает единицу от крайней части ip-адреса
            int sub = Int32.Parse(subs[3]);
            if (sub > 0)
            {
                sub -= 1;
                subs[3] = sub.ToString();
            }
            return String.Join(".", subs);
        }

        public static string GetSubnetsCount(string subnetMask)
        {
            // считает все вхождения единиц в 3-й и 4-й части ip-адреса в двоичной форме
            // затем возводит двойку в полученное значение
            string[] subs = subnetMask.Split('.');
            int subnetsCount = subs[2].Count(s => s == '1') + subs[3].Count(s => s == '1');
            double subnets = Math.Pow(2, subnetsCount);
            return subnets.ToString();
        }

        public static string GetHostsCount(string subnetMask)
        {
            // считает все вхождения нулей в 3-й и 4-й части ip-адреса в двоичной форме
            // затем возводит двойку в полученное значение
            string[] subs = subnetMask.Split('.');
            int hostsCount = subs[2].Count(h => h == '0') + subs[3].Count(h => h == '0');
            double hosts = Math.Pow(2, hostsCount);
            hosts -= 2; // 1 for broadcast, 1 for subnet
            return hosts.ToString();
        }

        public static string GetReverseIp(string ipAddress)
        {
            // побитовое реверсирование ip-адреса
            string[] reverseIp = ipAddress.Split('.');
            uint[] reverseSubs = new uint[4];
            for (int i = 0; i < 4; i++)
            {
                reverseSubs[i] = (byte)~UInt32.Parse(reverseIp[i]);
            }
            return String.Join(".", reverseSubs);
        }

        public static string GetNetworkAddress(string ipAddress, string subnetMask)
        {
            // получает адрес сети, путём операции умножения битов (AND)
            // между ip-адресом и маской сети
            string[] subnets = subnetMask.Split('.');
            string[] ips = ipAddress.Split('.');
            uint[] networkAddress = new uint[4];
            for (int i = 0; i < 4; i++)
            {
                uint tempSub = UInt32.Parse(subnets[i]);
                uint tempIp = UInt32.Parse(ips[i]);
                networkAddress[i] = tempSub & tempIp;
            }
            return String.Join(".", networkAddress);
        }

        public static string GetBroadcastAddress(string networkAddress, string reverseSubnetMask)
        {
            // получает адрес сети, путём операции складывания битов (OR)
            // между ip-адресом и обратной маской сети
            string[] reverseSubnets = reverseSubnetMask.Split('.');
            string[] net = networkAddress.Split('.');
            uint[] broadcastAddress = new uint[4];
            for (int i = 0; i < 4; i++)
            {
                uint tempSub = UInt32.Parse(reverseSubnets[i]);
                uint tempNet = UInt32.Parse(net[i]);
                broadcastAddress[i] = tempNet | tempSub;
            }
            return String.Join(".", broadcastAddress);
        }

        public static string ConvertIpToHexadecimal(string ipAddress)
        {
            // конвертирование ip-адреса из десятичной формы в шестнадцатеричную
            return String.Join(".", (ipAddress.Split('.')
            .Select(ip => Convert.ToString(Int32.Parse(ip), 16)
            .PadLeft(2, '0').ToUpper())).ToArray());            
        }

        public static string ConvertIpToBinary(string ipAddress)
        {
            // конвертирование ip-адреса из десятичной формы в двоичную
            return String.Join(".", (ipAddress.Split('.')
                .Select(ip => Convert.ToString(Int32.Parse(ip), 2)
                .PadLeft(8, '0'))).ToArray());
        }

        public static string GetSubnetMaskPrefix(string subnetMask)
        {
            // считает все вхождения единиц в ip-адресе в двоичной форме
            int mask = subnetMask.Count(b => b == '1');
            return mask.ToString();
        }
        
        public static bool isInputCorrect(string ipAddress)
        {
            // проверяет корректность ввода ip-адреса
            bool isCorrect = true;

            try
            {
                String.Join(".", (ipAddress.Split('.')
               .Select(ipAddress => Convert.ToString(Int32.Parse(ipAddress), 2)
               .PadLeft(8, '0'))).ToArray());
            }
            catch (FormatException)
            {
                isCorrect = false;
                MessageBox.Show("Неверный формат IP адреса!");
                return isCorrect;
            }

            string[] subStrings = ipAddress.Split('.');
            if (subStrings.Length == 4)
            {
                for (int i = 0; i < subStrings.Length; i++)
                {
                    if (!(Int32.Parse(subStrings[i]) > 0 && Int32.Parse(subStrings[i]) <= 255))
                    {
                        isCorrect = false;
                        MessageBox.Show("Неверный формат IP адреса!");
                        return isCorrect;
                    }
                }
                isCorrect = true;
                return isCorrect;
            }
            else
            {
                isCorrect = false;
                MessageBox.Show("Неверный формат IP адреса!");
                return isCorrect;
            }
        }
    }
}
