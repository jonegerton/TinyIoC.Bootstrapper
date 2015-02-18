
﻿//===============================================================================
// TinyIoC.Bootstrapper
//
// A bootstrapper implementation the TinyIoC Inversion of Control container
//
// https://github.com/jonegerton/TinyIoC.Bootstrapper
//===============================================================================
// Copyright © Jon Egerton.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================


using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace TinyIoC
{


    /// <summary>
    /// TinyIoC bootstrapper. Handles auto registration.
    /// </summary>
    public interface IBootstrapper
    {
        void ConfigureApplicationContainer(TinyIoCContainer container);
    }

    /// <summary>
    /// Default TinyIoC bootstrapper. Handles auto registration to cover most cases. 
    /// </summary>
    /// <remarks>
    /// Inherit from this class to add additional Ignored Assemblies and Types, and to add custom registrations.
    /// </remarks>
    public class Bootstrapper : IBootstrapper
    {

        //Elements of this are based on code in Nancy here:
        //https://github.com/NancyFx/Nancy/blob/6ceb54daec2dc230ab6fe55b367d3837e262c1db/src/Nancy/DefaultNancyBootstrapper.cs

        
        /// <summary> 
        /// Default assemblies that are ignored for registration 
        /// </summary> 
        public static IEnumerable<Func<string, bool>> DefaultAutoRegisterIgnoredAssemblies = new Func<string, bool>[] 
            { 
                asm => asm.StartsWith("Microsoft.", StringComparison.InvariantCulture), 
                asm => asm.StartsWith("System.", StringComparison.InvariantCulture), 
                asm => asm.StartsWith("System,", StringComparison.InvariantCulture), 
                asm => asm.StartsWith("mscorlib,", StringComparison.InvariantCulture),    
            };

        /// <summary> 
        /// Default types that are ignored for auto registration
        /// </summary>
        public static IEnumerable<Func<Type, bool>> DefaultAutoRegisterIgnoredTypes = new Func<Type, bool>[] 
        { 
            typ => typ.FullName.StartsWith("TinyIoC.", StringComparison.InvariantCulture),
        };

         
        /// <summary>
        /// Default path to search for assemblies to index.
        /// </summary>
        public static string DefaultSearchPath
        {
            get {
                return new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).LocalPath;
            }
        }

        /// <summary> 
        /// Gets the assemblies names to ignore when autoregistering the application container 
        /// Return true from the delegate to ignore that particular assembly, returning true 
        /// does not mean the assembly *will* be included, a false from another delegate will 
        /// take precedence. 
        /// </summary> 
        protected virtual IEnumerable<Func<string, bool>> AutoRegisterIgnoredAssemblies
        {
            get { return DefaultAutoRegisterIgnoredAssemblies; }
        }


        /// <summary> 
        /// Gets the types to ignore when autoregistering the application container 
        /// Return true from the delegate to ignore that particular type, returning true 
        /// does not mean the type *will* be included, a false from another delegate will 
        /// take precedence. 
        /// </summary> 
        protected virtual IEnumerable<Func<Type, bool>> AutoRegisterIgnoredTypes
        {
            get { return DefaultAutoRegisterIgnoredTypes; }
        }
        

        /// <summary>
        /// Gets the path to search for assemblies to index.
        /// </summary>
        protected virtual string AutoRegisterSearchPath
        {
            get {return DefaultSearchPath;}
        }

        /// <summary>
        /// Configures the container using AutoRegister
        /// </summary>
        /// <param name="container">Container instance</param>
        public virtual void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            AutoRegister(container, AutoRegisterIgnoredAssemblies, AutoRegisterIgnoredTypes);
        }

        /// <summary>
        /// Extra assemblies to include in mapping that might not be easily discoverable
        /// </summary>
        public List<Assembly> AddedAssemblies { get; set; }

        private IEnumerable<Assembly> LoadAssemblies(IEnumerable<Func<string, bool>> ignoredAssemblies)
        {
            List<Assembly> allAssemblies = new List<Assembly>();

            //Get assemblies in the AppDomain
            allAssemblies.AddRange(System.AppDomain.CurrentDomain.GetAssemblies());
            
            //Get list of files
            var files = Directory.GetFiles(AutoRegisterSearchPath, "*.dll");

            //A function to normalize file paths
            Func<string, string> normalizePath = s => new Uri(s).LocalPath.ToLowerInvariant();

            //Filter files list (so we don't load things we don't require)
            files = files.Where(f => !ignoredAssemblies.Any(ia => ia(Path.GetFileName(f)))).ToArray();


            //Add files found (if we haven't already got them)
            foreach (string dll in files)
            {
                var dllPath = normalizePath(dll);
                if (allAssemblies.All(a => normalizePath(a.Location) != dllPath))
                    allAssemblies.Add(Assembly.LoadFile(dll));
            }


            //Include the added assemblies
            if (AddedAssemblies != null)
            {
                foreach (var asm in AddedAssemblies)
                {
                    var asmPath = normalizePath(asm.Location);
                    if (allAssemblies.All(a => normalizePath(a.Location) != asmPath))
                        allAssemblies.Add(Assembly.LoadFile(asm.Location));
                }
            }

            //Apply filters again across full set of assemblies
            var assemblies = allAssemblies.Where(a => !ignoredAssemblies.Any(ia => ia(a.FullName))).ToList();

            return assemblies;

        }

        /// <summary>
        /// Executes auto registation with the given container.
        /// </summary>
        /// <param name="container">Container instance</param>
        private void AutoRegister(TinyIoCContainer container, IEnumerable<Func<string, bool>> ignoredAssemblies, IEnumerable<Func<Type, bool>> ignoredTypes)
        {
            container.AutoRegister(LoadAssemblies(ignoredAssemblies),
                DuplicateImplementationActions.RegisterMultiple,
                t => !ignoredTypes.Any(it => it(t)));

        }

    }
}
