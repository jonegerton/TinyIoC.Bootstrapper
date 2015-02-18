using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyIoC
{
    /// <summary>
    /// Overrides Bootstrapper to give examples of override pattern
    /// </summary>
    public class ExampleBootstrapper : Bootstrapper
    {
        /// <summary>
        /// Override and add to the list of ignored assemblies
        /// </summary>
        protected override IEnumerable<Func<string, bool>> AutoRegisterIgnoredAssemblies
        {
            get
            {

                //The ignored assemblies list uses the assembly names.
                //This is so that the bootstrapper can eliminate then without ever loading them where possible.

                var assmblys = base.AutoRegisterIgnoredAssemblies;

                return assmblys.Concat(new Func<string, bool>[]
                {
                    asm => asm.StartsWith("EntityFramework.", StringComparison.InvariantCulture),
                    asm => asm.StartsWith("AutoMapper.", StringComparison.InvariantCulture)
                });

            }
        }

        /// <summary>
        /// Override and add to the list of ignored types
        /// </summary>
        protected override IEnumerable<Func<Type, bool>> AutoRegisterIgnoredTypes
        {
            get
            {
                //The ignored types list uses the actual types.
                //This enables filtering by any of the properties defined on the type for max flexiblity

                var typs = base.AutoRegisterIgnoredTypes;

                return typs.Concat(new Func<Type, bool>[]
                {
                    typ => typ.FullName.StartsWith("Modernizr")
                });
            }
        }

        /// <summary>
        /// Override the default search location (defaults to the application execution location)
        /// </summary>
        protected override string AutoRegisterSearchPath
        {
            get
            {
                //This might be required when debugging in IISExpress for example, where
                //the execution location is a temporary location and not the bin folder.

                //Just set to Temp for this example
                return System.IO.Path.GetTempPath();
            }
        }

        /// <summary>
        /// Override the default configure, to allow space for custom bindings and instance control.
        /// </summary>
        /// <param name="container"></param>
        public override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            //This is where to add any custom registrations
            container.Register<IDbConnection, SqlConnection>().AsMultiInstance();
        }
    }
}
