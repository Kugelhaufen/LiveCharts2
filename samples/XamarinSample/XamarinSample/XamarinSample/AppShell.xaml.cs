﻿using System;
using Xamarin.Forms;

namespace XamarinSample
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        private bool isLoaded = false;

        public AppShell()
        {
            InitializeComponent();
            SizeChanged += AppShell_SizeChanged;
        }

        private void AppShell_SizeChanged(object sender, EventArgs e)
        {
            if (isLoaded) return;
            isLoaded = true;

            var samples = ViewModelsSamples.Index.Samples;

            foreach (var item in samples)
            {
                var shell_section = new ShellSection { Title = item };

                if (item.ToLower().Contains("tooltip")) continue;
                var t = Type.GetType($"XamarinSample.{item.Replace('/', '.')}.View");
                var i = Activator.CreateInstance(t);

                Routing.RegisterRoute(item, t);

                shell_section.Items.Add(new ShellContent() { Content = i });
                Items.Add(shell_section);
            }
        }

        private async void OnMenuItemClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
