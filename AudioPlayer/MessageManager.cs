using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AudioPlayer
{
    public class MessageManager
    {
        private static MessageManager? instance;
        public static MessageManager Instance 
        { 
            get 
            { 
                if (instance == null)
                {
                    instance = new MessageManager();
                }
                return instance; 
            } 
        }

        private MessageManager() { }
        
        public void Warning(string message)
            => MessageBox.Show(message, string.Empty, MessageBoxButton.OK, MessageBoxImage.Information);
    }
}