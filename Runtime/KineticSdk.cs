using System;
using IO.Swagger.Api;
using IO.Swagger.Model;
using Kinetic.Sdk.Helpers;
using Kinetic.Sdk.Transactions;
using Solana.Unity.Rpc.Types;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Kinetic.Sdk
{

    /// <summary>
    /// The KineticSdk is the main entry point and handles communication with the Kinetic API
    /// </summary>
    public class KineticSdk
    {
        private readonly AccountApi _accountApi;
        private readonly AirdropApi _airdropApi;
        private readonly AppApi _appApi;
        private readonly TransactionApi _transactionApi;
        
        private readonly KineticSdkConfig _sdkConfig;

        public Solana Solana { get; private set; }
        public AppConfig Config { get; private set; }

        private KineticSdk(KineticSdkConfig config)
        {
            _sdkConfig = config;
            var basePath = config.Endpoint.ParseEndpoint();
            
            _accountApi = new AccountApi(basePath);
            _airdropApi = new AirdropApi(basePath);
            _appApi = new AppApi(basePath);
            _transactionApi = new TransactionApi(basePath);
        }
        
        #region Transactions

        public AppTransaction CreateAccount(Keypair owner, string mint = null, Commitment commitment = default)
        {
            if (Config is null)
            {
                throw new Exception("AppConfig not initialized");
            }

            mint ??= Config.Mint.PublicKey;

            var pt = PrepareTransaction(mint);
            
            var tx = TransactionHelper.CreateAccountTransaction(
                Config.Mint.AddMemo,
                appIndex: Config.App.Index,
                pt.LatestBlockhash, 
                pt.MintFeePayer, 
                pt.MintPublicKey,
                owner);

            var request = new CreateAccountRequest
            {
                Environment = Config.Environment.Name,
                Index = Config.App.Index,
                Mint = mint,
                Tx = tx
            };

            var res = _accountApi.CreateAccount(request);
            return res;
        }

        #endregion
        
        #region Initialization

        private AppConfig Init()
        {
            try
            {
                Config = _appApi.GetAppConfig(_sdkConfig.Environment, _sdkConfig.Index);
                _sdkConfig.SolanaRpcEndpoint = _sdkConfig.SolanaRpcEndpoint != null
                    ? _sdkConfig.SolanaRpcEndpoint.GetSolanaRpcEndpoint() 
                    : Config.Environment.Cluster.Endpoint.GetSolanaRpcEndpoint();
                Solana = new Solana(_sdkConfig.SolanaRpcEndpoint, _sdkConfig.Logger);
                _sdkConfig?.Logger?.Log(
                    $"KineticSdk: endpoint '{_sdkConfig.Endpoint}', " +
                    $"environment '{_sdkConfig.Environment}'," +
                    $" index: {Config.App.Index}"
                );
                return Config;
            }
            catch (Exception e)
            {
                Debug.LogError("Error initializing Server." + e.Message);
                throw;
            }
        }

        public static KineticSdk Setup(KineticSdkConfig config)
        {
            var sdk = new KineticSdk(config);
            try
            {
                sdk.Init();
                config.Logger?.Log("KineticSdk: Setup done.");
                return sdk;
            }
            catch (Exception e)
            {
                Debug.LogError("KineticSdk: Error setting up SDK." + e.Message);
                throw;
            }
        }
        
        #endregion
        
        #region Utils
        
        private PreTransaction PrepareTransaction(string mint){
            if (Config is null)
            {
                throw new Exception("AppConfig not initialized");
            }
            mint ??= Config.Mint.PublicKey;
            var found = Config.Mints.Find((item) => item.PublicKey == mint);
            if (found is null)
            {
                throw new Exception("Mint not found");
            }

            var latestBlockhashResponse = _transactionApi.GetLatestBlockhash(Config.Environment.Name, Config.App.Index);

            return new PreTransaction
            {
                MintDecimals = found.Decimals,
                MintPublicKey = found.PublicKey,
                MintFeePayer = found.FeePayer,
                LatestBlockhash = latestBlockhashResponse.Blockhash,
                LastValidBlockHeight = latestBlockhashResponse.LastValidBlockHeight
            };
        }
        
        #endregion
    }
    
}