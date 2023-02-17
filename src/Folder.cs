﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Zhai.VideoView;

namespace Zhai.PictureView
{
    internal class Folder : ObservableCollection<Video>
    {
        public DirectoryInfo Current { get; }

        public List<DirectoryInfo> Borthers { get; private set; }

        public bool IsAccessed => CanAccess(Current);

        public Folder(DirectoryInfo dir, List<DirectoryInfo> borthers = null)
        {
            if (!dir.Exists)
                throw new DirectoryNotFoundException();

            Current = dir;

            if (borthers != null)
            {
                Borthers = borthers;
            }
        }


        readonly Func<FileInfo, bool> PictureSupportExpression = file => (file.Attributes & (FileAttributes.Hidden | FileAttributes.System | FileAttributes.Temporary)) == 0 && VideoSupport.IsSupported(file.FullName);

        public async Task LoadAsync()
        {
            var files = Current.EnumerateFiles().Where(PictureSupportExpression);

            if (files.Any())
            {
                await Task.Run(() =>
                {
                    foreach (var file in files)
                    {
                        Application.Current.Dispatcher.Invoke(() => Add(new Video(file.FullName)));
                    };

                });

                if (Borthers == null)
                {
                    LoadBorthers();
                }
            }
        }

        public void LoadBorthers()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                if (Current.Parent != null && CanAccess(Current.Parent))
                {
                    Borthers = Current.Parent.EnumerateDirectories().Where(dir =>
                    {
                        var isSecurity = CanAccess(dir);

                        if (isSecurity)
                        {
                            return dir.EnumerateFiles().Where(PictureSupportExpression).Any();
                        }

                        return false;

                    }).ToList();
                }
            });
        }

        private bool CanAccess(DirectoryInfo dir = null)
        {
            return !dir.GetAccessControl(AccessControlSections.Access).AreAccessRulesProtected;



            //var currentUserIdentity = Path.Combine(Environment.UserDomainName, Environment.UserName);

            //DirectorySecurity directorySecurity = dir.GetAccessControl();

            //var userAccessRules = directorySecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)).OfType<FileSystemAccessRule>().Where(i => i.IdentityReference.Value == currentUserIdentity).ToList();

            //return userAccessRules.Any(i => i.AccessControlType == AccessControlType.Deny);
        }

        public bool GetNext(out DirectoryInfo dir)
        {
            dir = Current;

            if (Borthers == null || Borthers.Count == 1)
                return false;

            var item = Borthers.Find(t => t.FullName == Current.FullName);

            var index = Borthers.IndexOf(item);

            index += 1;

            if (index > Borthers.Count - 1)
            {
                index = 0;
            }

            dir = Borthers[index];

            return true;
        }

        public bool GetPrev(out DirectoryInfo dir)
        {
            dir = Current;

            if (Borthers == null || Borthers.Count == 1)
                return false;

            var item = Borthers.Find(t => t.FullName == Current.FullName);

            var index = Borthers.IndexOf(item);

            index -= 1;

            if (index < 0)
            {
                index = Borthers.Count - 1;
            }

            dir = Borthers[index];

            return true;
        }

        public void Cleanup()
        {
            if (this.Any())
            {
                foreach (var item in this)
                {
                    item.Cleanup();
                }

                this.Clear();
            }
        }
    }
}
