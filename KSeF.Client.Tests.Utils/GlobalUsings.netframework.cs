// Guard class (Compatibility) is available globally on ALL TFMs:
// On net48: provides polyfill implementations for argument validation
// On net8.0+: provides inline forwarding to built-in methods
global using KSeF.Client.Tests.Utils.Compatibility;

#if NETFRAMEWORK
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;
#endif
