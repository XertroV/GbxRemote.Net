﻿using GbxRemoteNet.Structs;
using GbxRemoteNet.XmlRpc;
using GbxRemoteNet.XmlRpc.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GbxRemoteNet {
    public partial class GbxRemoteClient {
        #region System Methods
        /// <summary>
        /// Return an array of all available XML-RPC methods on this server.
        /// </summary>
        /// <returns></returns>
        public async Task<string[]> SystemListMethodsAsync() {
            return (string[])XmlRpcTypes.ToNativeValue<string>((XmlRpcArray)
                await CallOrFaultAsync("system.listMethods")
            );
        }

        /// <summary>
        /// Given the name of a method, return an array of legal signatures. Each signature is an array of strings. The first item of each signature is the return type, and any others items are parameter types.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public async Task<string[][]> SystemMethodSignatureAsync(string method) {
            return XmlRpcTypes.ToNative2DArray<string>((XmlRpcArray)
                await CallOrFaultAsync("system.methodSignature", Arguments(method))
            );
        }

        /// <summary>
        /// Given the name of a method, return a help string.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public async Task<string> SystemMethodHelpAsync(string method) {
            return (string)XmlRpcTypes.ToNativeValue<string>(
                await CallOrFaultAsync("system.methodHelp", Arguments(method))
            );
        }

        // todo: multicall
        #endregion

        #region Session Methods
        /// <summary>
        /// Allow user authentication by specifying a login and a password, to gain access to the set of functionalities corresponding to this authorization level.
        /// </summary>
        /// <param name="login"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<bool> AuthenticateAsync(string login, string password) {
            return (bool)XmlRpcTypes.ToNativeValue<bool>(
                await CallOrFaultAsync("Authenticate", Arguments(login, password))
            );
        }

        /// <summary>
        /// Change the password for the specified login/user. Only available to SuperAdmin.
        /// </summary>
        /// <param name="login"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<bool> ChangeAuthPasswordAsync(string login, string password) {
            return (bool)XmlRpcTypes.ToNativeValue<bool>(
                await CallOrFaultAsync("ChangeAuthPassword", Arguments(login, password))
            );
        }

        /// <summary>
        /// Allow the GameServer to call you back.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> EnableCallbacksAsync(bool enable) {
            return (bool)XmlRpcTypes.ToNativeValue<bool>(
                await CallOrFaultAsync("EnableCallbacks", Arguments(enable))
            );
        }

        /// <summary>
        /// Define the wanted api.
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public async Task<bool> SetApiVersionAsync(string version) {
            return (bool)XmlRpcTypes.ToNativeValue<bool>(
                await CallOrFaultAsync("SetApiVersion", Arguments(version))
            );
        }
        #endregion

        #region Server
        /// <summary>
        /// Returns a struct with the Name, TitleId, Version, Build and ApiVersion of the application remotely controlled.
        /// </summary>
        /// <returns></returns>
        public async Task<VersionStruct> GetVersionAsync() {
            return (VersionStruct)XmlRpcTypes.ToNativeValue<VersionStruct>(
                await CallOrFaultAsync("GetVersion")
            );
        }

        /// <summary>
        /// Returns the current status of the server.
        /// </summary>
        /// <returns></returns>
        public async Task<StatusStruct> GetStatusAsync() {
            return (StatusStruct)XmlRpcTypes.ToNativeValue<StatusStruct>(
                await CallOrFaultAsync("GetStatus")
            );
        }

        /// <summary>
        /// Quit the application. Only available to SuperAdmin.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> QuitGameAsync() {
            return (bool)XmlRpcTypes.ToNativeValue<bool>(
                await CallOrFaultAsync("QuitGame")
            );
        }
        #endregion

        #region Votes
        /// <summary>
        /// Call a vote for a cmd. The command is a XML string corresponding to an XmlRpc request. Only available to Admin.
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public async Task<bool> CallVoteAsync(string cmd) {
            return (bool)XmlRpcTypes.ToNativeValue<bool>(
                await CallOrFaultAsync("CallVote", Arguments(cmd))
            );
        }

        /// <summary>
        /// Extended call vote. Same as CallVote, but you can additionally supply specific parameters for this vote: a ratio, a time out and who is voting. Special timeout values: a ratio of '-1' means default; a timeout of '0' means default, '1' means indefinite; Voters values: '0' means only active players, '1' means any player, '2' is for everybody, pure spectators included. Only available to Admin.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="ratio"></param>
        /// <param name="timeout"></param>
        /// <param name="who"></param>
        /// <returns></returns>
        public async Task<bool> CallVoteExAsync(string cmd, double ratio, int timeout, int who) {
            return (bool)XmlRpcTypes.ToNativeValue<bool>(
                await CallOrFaultAsync("CallVoteEx", Arguments(cmd, ratio, timeout, who))
            );
        }

        /// <summary>
        /// Used internally by game.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> InternalCallVoteAsync() {
            return (bool)XmlRpcTypes.ToNativeValue<bool>(
                await CallOrFaultAsync("InternalCallVote")
            );
        }

        /// <summary>
        /// Cancel the current vote. Only available to Admin.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CancelVoteVoteAsync() {
            return (bool)XmlRpcTypes.ToNativeValue<bool>(
                await CallOrFaultAsync("CancelVoteVote")
            );
        }

        /// <summary>
        /// Returns the vote currently in progress. The returned structure is { CallerLogin, CmdName, CmdParam }.
        /// </summary>
        /// <returns></returns>
        public async Task<CurrentCallVoteStruct> GetCurrentCallVoteAsync() {
            return (CurrentCallVoteStruct)XmlRpcTypes.ToNativeValue<CurrentCallVoteStruct>(
                await CallOrFaultAsync("GetCurrentCallVote")
            );
        }

        /// <summary>
        /// Set a new timeout for waiting for votes. A zero value disables callvote. Only available to Admin. Requires a map restart to be taken into account.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<bool> SetCallVoteTimeOutAsync(int timeout) {
            return (bool)XmlRpcTypes.ToNativeValue<bool>(
                await CallOrFaultAsync("SetCallVoteTimeOut", Arguments(timeout))
            );
        }

        /// <summary>
        /// Get the current and next timeout for waiting for votes. The struct returned contains two fields 'CurrentValue' and 'NextValue'.
        /// </summary>
        /// <returns></returns>
        public async Task<CallVoteTimeOutStruct> GetCallVoteTimeOutAsync() {
            return (CallVoteTimeOutStruct)XmlRpcTypes.ToNativeValue<CallVoteTimeOutStruct>(
                await CallOrFaultAsync("GetCallVoteTimeOut")
            );
        }

        /// <summary>
        /// Set a new default ratio for passing a vote. Must lie between 0 and 1. Only available to Admin.
        /// </summary>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public async Task<bool> SetCallVoteRatioAsync(double ratio) {
            return (bool)XmlRpcTypes.ToNativeValue<bool>(
                await CallOrFaultAsync("SetCallVoteRatio", Arguments(ratio))
            );
        }

        /// <summary>
        /// Get the current default ratio for passing a vote. This value lies between 0 and 1.
        /// </summary>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public async Task<double> GetCallVoteRatioAsync() {
            return (double)XmlRpcTypes.ToNativeValue<double>(
                await CallOrFaultAsync("GetCallVoteRatio")
            );
        }

        /// <summary>
        /// Set the ratios list for passing specific votes. The parameter is an array of structs {string Command, double Ratio}, ratio is in [0,1] or -1 for vote disabled. Only available to Admin.
        /// </summary>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public async Task<bool> SetCallVoteRatiosAsync(CallVoteRatioStruct[] ratios) {
            return (bool)XmlRpcTypes.ToNativeValue<bool>(
                await CallOrFaultAsync("SetCallVoteRatios", Arguments(ratios))
            );
        }

        /// <summary>
        /// Get the current ratios for passing votes.
        /// </summary>
        /// <returns></returns>
        public async Task<CallVoteRatioStruct[]> GetCallVoteRatiosAsync() {
            return (CallVoteRatioStruct[])XmlRpcTypes.ToNativeValue<CallVoteRatioStruct>(
                await CallOrFaultAsync("GetCallVoteRatios")
            );
        }
        #endregion

        #region Chat
        /// <summary>
        /// Send a text message to all clients without the server login. Only available to Admin.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> ChatSendServerMessageAsync(string message) {
            return (bool)XmlRpcTypes.ToNativeValue<bool>(
                await CallOrFaultAsync("ChatSendServerMessage", Arguments(message))
            );
        }

        public async Task<bool> ChatSendServerMessageToLanguageAsync(LanguageStruct[] lang, string message) {
            return (bool)XmlRpcTypes.ToNativeValue<bool>(
                await CallOrFaultAsync("ChatSendServerMessage", Arguments(lang, message))
            );
        }
        #endregion
    }
}
