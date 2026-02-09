// Klasa Guard (Compatibility) jest dostępna globalnie na wszystkich TFM:
// Na net48: zapewnia implementacje polyfill do walidacji argumentów
// Na net8.0+: zapewnia inline przekierowanie do wbudowanych metod
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
