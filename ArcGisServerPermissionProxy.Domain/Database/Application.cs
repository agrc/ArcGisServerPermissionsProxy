﻿namespace ArcGisServerPermissionProxy.Domain.Database
{
    public class Application
    {
        private string _name;

        public Application(string name, string password)
        {
            Name = name;
            Password = password;
        }

        public string Name
        {
            get { return _name == null ? null : _name.ToLowerInvariant(); }
            private set
            {
                if (value == null || string.IsNullOrEmpty(value))
                    _name = null;
                else
                {
                    _name = value;
                }
            }
        }
        public string Password { get; set; }
    }
}