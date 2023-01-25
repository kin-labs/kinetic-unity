﻿using System.Collections.Generic;

namespace Solana.Unity.Rpc.Messages
{
    /// <summary>
    /// This class represents multiple JsonRpcRequest objects and is used for making 
    /// a of batch requests in a single HTTP request.
    /// </summary>
    public class JsonRpcBatchRequest : List<JsonRpcRequest>
    {

        // https://docs.solana.com/developing/clients/jsonrpc-api
        // Requests can be sent in batches by sending an array of JSON-RPC request objects as the data for a single POST.

    }
}
