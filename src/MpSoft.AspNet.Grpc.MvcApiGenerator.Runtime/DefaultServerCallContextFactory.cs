#region using
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#endregion using

namespace MpSoft.AspNet.Grpc.MvcApiGenerator.Runtime
{
	//Highly inspirated by Grpc.AspNetCore.Server.Internal.HttpContextServerCallContext
	sealed class DefaultServerCallContextFactory:IServerCallContextFactory
	{
		static readonly AuthContext UnauthenticatedContext = new AuthContext(null,new Dictionary<string,List<AuthProperty>>());

		public ServerCallContext Create(HttpContext httpContext)
			=> new HttpGrpcServerCallContext(httpContext);

		class HttpGrpcServerCallContext:ServerCallContext
		{
			readonly HttpContext _httpContext;
			internal HttpGrpcServerCallContext(HttpContext httpContext)
				=> _httpContext=httpContext;

			protected override string MethodCore => _httpContext.Request.Path.Value;

			protected override string HostCore => _httpContext.Request.Host.Value;

			string _peer;
			protected override string PeerCore
			{
				get
				{
					if (_peer==null)
					{
						ConnectionInfo connection = _httpContext.Connection;
						if (connection.RemoteIpAddress!=null)
							_peer=connection.RemoteIpAddress.AddressFamily switch
							{
								AddressFamily.InterNetwork => "ipv4:"+connection.RemoteIpAddress?.ToString()+":"+connection.RemotePort,
								AddressFamily.InterNetworkV6 => "ipv6:["+connection.RemoteIpAddress?.ToString()+"]:"+connection.RemotePort,
								_ => "unknown:"+connection.RemoteIpAddress?.ToString()+":"+connection.RemotePort,
							};
					}
					return _peer;
				}
			}

			protected override DateTime DeadlineCore => DateTime.MaxValue;

			Metadata _requestHeaders;
			protected override Metadata RequestHeadersCore
			{
				get
				{
					if (_requestHeaders==null)
					{
						_requestHeaders=new Metadata();
						foreach (KeyValuePair<string,StringValues> header in _httpContext.Request.Headers)
							_requestHeaders.Add(header.Key,header.Value);
					}
					return _requestHeaders;
				}
			}

			protected override CancellationToken CancellationTokenCore => _httpContext.RequestAborted;

			Metadata _responseTrailers;
			protected override Metadata ResponseTrailersCore => _responseTrailers??=new Metadata();

			protected override Status StatusCore { get; set; }
			protected override WriteOptions WriteOptionsCore { get; set; }

			AuthContext _authContext;
			protected override AuthContext AuthContextCore
			{
				get
				{
					if (_authContext==null)
					{
						X509Certificate2 clientCertificate = _httpContext.Connection.ClientCertificate;
						if (clientCertificate==null)
						{
							_authContext=UnauthenticatedContext;
						}
						else
						{
							_authContext=GrpcProtocolHelpers.CreateAuthContext(clientCertificate);
						}
					}
					return _authContext;
				}
			}

			protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions options)
			{
				throw new NotImplementedException();
			}

			protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
			{
				if (_httpContext.Response.HasStarted)
					throw new InvalidOperationException("Response headers can only be sent once per call.");
				if (responseHeaders!=null)
					foreach (Metadata.Entry entry in responseHeaders)
							_httpContext.Response.Headers.Append(entry.Key,entry.IsBinary ? Convert.ToBase64String(entry.ValueBytes) : entry.Value);
				return _httpContext.Response.BodyWriter.FlushAsync().AsTask();
			}
		}
	}

	static class GrpcProtocolHelpers
	{
		internal static AuthContext CreateAuthContext(X509Certificate2 clientCertificate)
		{
			Dictionary<string,List<AuthProperty>> properties2 = new Dictionary<string,List<AuthProperty>>(StringComparer.Ordinal);
			string peerIdentityPropertyName = null;
			string[] dnsFromExtensions = X509CertificateHelpers.GetDnsFromExtensions(clientCertificate);
			foreach (string dnsName in dnsFromExtensions)
			{
				AddProperty(properties2,"x509_subject_alternative_name",dnsName);
				if (peerIdentityPropertyName==null)
				{
					peerIdentityPropertyName="x509_subject_alternative_name";
				}
			}
			string commonName = clientCertificate.GetNameInfo(X509NameType.SimpleName,forIssuer: false);
			if (commonName!=null)
			{
				AddProperty(properties2,"x509_common_name",commonName);
				if (peerIdentityPropertyName==null)
				{
					peerIdentityPropertyName="x509_common_name";
				}
			}
			return new AuthContext(peerIdentityPropertyName,properties2);
			static void AddProperty(Dictionary<string,List<AuthProperty>> properties,string name,string value)
			{
				if (!properties.TryGetValue(name,out var values))
				{
					values=(properties[name]=new List<AuthProperty>());
				}
				values.Add(AuthProperty.Create(name,Encoding.UTF8.GetBytes(value)));
			}
		}
	}

	static class X509CertificateHelpers
	{
		static class X509SubjectAlternativeNameConstants
		{
			public const string Oid = "2.5.29.17";

			private static readonly string s_identifier;

			private static readonly char s_delimiter;

			private static readonly string s_separator;

			private static readonly bool s_successfullyInitialized;

			private static readonly Exception s_initializationException;

			public static string Identifier
			{
				get
				{
					EnsureInitialized();
					return s_identifier;
				}
			}

			public static char Delimiter
			{
				get
				{
					EnsureInitialized();
					return s_delimiter;
				}
			}

			public static string Separator
			{
				get
				{
					EnsureInitialized();
					return s_separator;
				}
			}

			private static void EnsureInitialized()
			{
				if (!s_successfullyInitialized)
				{
					throw new FormatException(string.Format(CultureInfo.InvariantCulture,"There was an error detecting the identifier, delimiter, and separator for X509CertificateClaims on this platform.{0}Detected values were: Identifier: '{1}'; Delimiter:'{2}'; Separator:'{3}'",Environment.NewLine,s_identifier,s_delimiter,s_separator),s_initializationException);
				}
			}

			static X509SubjectAlternativeNameConstants()
			{
				byte[] x509ExtensionBytes = new byte[38]
				{
			48,
			36,
			130,
			21,
			110,
			111,
			116,
			45,
			114,
			101,
			97,
			108,
			45,
			115,
			117,
			98,
			106,
			101,
			99,
			116,
			45,
			110,
			97,
			109,
			101,
			130,
			11,
			101,
			120,
			97,
			109,
			112,
			108,
			101,
			46,
			99,
			111,
			109
				};
				try
				{
					string x509ExtensionFormattedString = new X509Extension("2.5.29.17",x509ExtensionBytes,critical: true).Format(multiLine: false);
					int delimiterIndex = x509ExtensionFormattedString.IndexOf("not-real-subject-name",StringComparison.Ordinal)-1;
					s_delimiter=x509ExtensionFormattedString[delimiterIndex];
					s_identifier=x509ExtensionFormattedString.Substring(0,delimiterIndex);
					int separatorFirstChar = delimiterIndex+"not-real-subject-name".Length+1;
					int separatorLength = 1;
					for (int i = separatorFirstChar+1;i<x509ExtensionFormattedString.Length&&x509ExtensionFormattedString[i]!=s_identifier![0];i++)
					{
						separatorLength++;
					}
					s_separator=x509ExtensionFormattedString.Substring(separatorFirstChar,separatorLength);
					s_successfullyInitialized=true;
				}
				catch (Exception ex)
				{
					s_successfullyInitialized=false;
					s_initializationException=ex;
				}
			}
		}

		internal static string[] GetDnsFromExtensions(X509Certificate2 cert)
		{
			X509ExtensionEnumerator enumerator = cert.Extensions.GetEnumerator();
			while (enumerator.MoveNext())
			{
				X509Extension ext = enumerator.Current;
				if (!(ext.Oid?.Value=="2.5.29.17"))
				{
					continue;
				}
				string asnString = ext.Format(multiLine: false);
				if (string.IsNullOrWhiteSpace(asnString))
				{
					return Array.Empty<string>();
				}
				string[] rawDnsEntries = asnString.Split(new string[1]
				{
					X509SubjectAlternativeNameConstants.Separator
				},StringSplitOptions.RemoveEmptyEntries);
				List<string> dnsEntries = new List<string>();
				for (int i = 0;i<rawDnsEntries.Length;i++)
				{
					string[] keyval = rawDnsEntries[i].Split(X509SubjectAlternativeNameConstants.Delimiter);
					if (string.Equals(keyval[0],X509SubjectAlternativeNameConstants.Identifier,StringComparison.Ordinal))
					{
						dnsEntries.Add(keyval[1]);
					}
				}
				return dnsEntries.ToArray();
			}
			return Array.Empty<string>();
		}
	}
}
