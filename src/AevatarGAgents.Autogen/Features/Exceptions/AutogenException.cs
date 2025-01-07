using System;

namespace AevatarGAgents.Autogen.Exceptions;

public class AutogenException(string autogenException) : Exception(autogenException);