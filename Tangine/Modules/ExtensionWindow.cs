﻿using System.Windows;

using Tangine.Habbo;
using Tangine.Network;

namespace Tangine.Modules
{
    public class ExtensionWindow : Window, IModule
    {
        private readonly TService _service;

        public IInstaller Installer { get; set; }
        public virtual bool IsStandalone { get; }
        
        public HGame Game => Installer.Game;
        public HGameData GameData => Installer.GameData;
        public IHConnection Connection => Installer.Connection;
        
        public ExtensionWindow()
        {
            _service = new TService(this);
        }

        void IModule.Synchronize(HGame game)
        {
            Synchronize(game);
            _service.Synchronize(game);
        }
        public virtual void Synchronize(HGame game)
        { }

        void IModule.Synchronize(HGameData gameData)
        {
            Synchronize(gameData);
            _service.Synchronize(gameData);
        }
        public virtual void Synchronize(HGameData gameData)
        { }

        void IModule.HandleOutgoing(DataInterceptedEventArgs e)
        {
            HandleOutgoing(e);
            _service.HandleOutgoing(e);
        }
        public virtual void HandleOutgoing(DataInterceptedEventArgs e)
        { }

        void IModule.HandleIncoming(DataInterceptedEventArgs e)
        {
            HandleIncoming(e);
            _service.HandleIncoming(e);
        }
        public virtual void HandleIncoming(DataInterceptedEventArgs e)
        { }

        public void Dispose()
        {
            Dispose(true);
        }
        public virtual void Dispose(bool disposing)
        { }
    }
}