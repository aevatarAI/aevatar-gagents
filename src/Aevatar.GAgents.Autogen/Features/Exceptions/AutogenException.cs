using System;

namespace Aevatar.GAgents.Autogen.Exceptions;

public class AutogenException(string autogenException) : Exception(autogenException);