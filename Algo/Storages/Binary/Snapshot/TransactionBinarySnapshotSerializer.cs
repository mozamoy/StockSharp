namespace StockSharp.Algo.Storages.Binary.Snapshot
{
	using System;
	using System.Runtime.InteropServices;

	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Implementation of <see cref="ISnapshotSerializer{TKey,TMessage}"/> in binary format for <see cref="ExecutionMessage"/>.
	/// </summary>
	public class TransactionBinarySnapshotSerializer : ISnapshotSerializer<long, ExecutionMessage>
	{
		//private const int _snapshotSize = 1024 * 10; // 10kb

		[StructLayout(LayoutKind.Sequential, Pack = 1/*, Size = _snapshotSize*/, CharSet = CharSet.Unicode)]
		private struct TransactionSnapshot
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
			public string SecurityId;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
			public string Portfolio;

			public long LastChangeServerTime;
			public long LastChangeLocalTime;

			public long OriginalTransactionId;
			public long TransactionId;

			public decimal OrderPrice;
			public long? OrderId;
			//public long OrderUserId;
			public decimal? OrderVolume;
			public byte? OrderType;
			//public byte OrderSide;
			public byte? OrderTif;

			public byte? IsSystem;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
			public string OrderStringId;

			public long? TradeId;
			public decimal? TradePrice;
			public decimal? TradeVolume;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
			public string BrokerCode;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
			public string ClientCode;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
			public string Comment;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
			public string SystemComment;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 200)]
			public string Error;

			public short? Currency;
			
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
			public string DepoName;

			public long? ExpiryDate;
			
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
			public string PortfolioName;

			public byte? IsMarketMaker;
			public byte Side;
			
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
			public string OrderBoardId;

			public decimal? VisibleVolume;
			public byte? OrderState;
			public long? OrderStatus;
			public decimal? Balance;
			
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
			public string UserOrderId;

			public byte? OriginSide;
			public long? Latency;
			public decimal? PnL;
			public decimal? Position;
			public decimal? Slippage;
			public decimal? Commission;
			public int? TradeStatus;
			
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
			public string TradeStringId;

			public decimal? OpenInterest;
			public byte? IsMargin;
		}

		Version ISnapshotSerializer<long, ExecutionMessage>.Version { get; } = new Version(2, 0);

		//int ISnapshotSerializer<long, ExecutionMessage>.GetSnapshotSize(Version version) => _snapshotSize;

		string ISnapshotSerializer<long, ExecutionMessage>.FileName => "transaction_snapshot.bin";

		byte[] ISnapshotSerializer<long, ExecutionMessage>.Serialize(Version version, ExecutionMessage message)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var snapshot = new TransactionSnapshot
			{
				SecurityId = message.SecurityId.ToStringId(),
				Portfolio = message.PortfolioName,
				LastChangeServerTime = message.ServerTime.To<long>(),
				LastChangeLocalTime = message.LocalTime.To<long>(),

				OriginalTransactionId = message.OriginalTransactionId,
				TransactionId = message.TransactionId,

				BrokerCode = message.BrokerCode,
				ClientCode = message.ClientCode,
				Comment = message.Comment,
				SystemComment = message.SystemComment,
				Currency = message.Currency == null ? (short?)null : (short)message.Currency.Value,
				// TODO
				//Condition = message.Condition,
				DepoName = message.DepoName,
				Error = message.Error?.Message,
				ExpiryDate = message.ExpiryDate?.To<long>(),
				PortfolioName = message.PortfolioName,
				IsMarketMaker = message.IsMarketMaker == null ? (byte?)null : (byte)(message.IsMarketMaker.Value ? 1 : 0),
				IsMargin = message.IsMargin == null ? (byte?)null : (byte)(message.IsMargin.Value ? 1 : 0),
				Side = (byte)message.Side,
				OrderId = message.OrderId,
				OrderStringId = message.OrderStringId,
				OrderBoardId = message.OrderBoardId,
				OrderPrice = message.OrderPrice,
				OrderVolume = message.OrderVolume,
				VisibleVolume = message.VisibleVolume,
				OrderType = message.OrderType == null ? (byte?)null : (byte)message.OrderType.Value,
				OrderState = message.OrderState == null ? (byte?)null : (byte)message.OrderState.Value,
				OrderStatus = message.OrderStatus,
				Balance = message.Balance,
				UserOrderId = message.UserOrderId,
				OriginSide = message.OriginSide == null ? (byte?)null : (byte)message.OriginSide.Value,
				Latency = message.Latency?.Ticks,
				PnL = message.PnL,
				Position = message.Position,
				Slippage = message.Slippage,
				Commission = message.Commission,
				TradePrice = message.TradePrice,
				TradeVolume = message.TradeVolume,
				TradeStatus = message.TradeStatus,
				TradeId = message.TradeId,
				TradeStringId = message.TradeStringId,
				OpenInterest = message.OpenInterest,
				IsSystem = message.IsSystem == null ? (byte?)null : (byte)(message.IsSystem.Value ? 1 : 0),
				OrderTif = message.TimeInForce == null ? (byte?)null : (byte)message.TimeInForce.Value,
			};

			var buffer = new byte[typeof(TransactionSnapshot).SizeOf()];

			var ptr = snapshot.StructToPtr();
			Marshal.Copy(ptr, buffer, 0, buffer.Length);
			Marshal.FreeHGlobal(ptr);

			return buffer;
		}

		ExecutionMessage ISnapshotSerializer<long, ExecutionMessage>.Deserialize(Version version, byte[] buffer)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));

			// Pin the managed memory while, copy it out the data, then unpin it
			using (var handle = new GCHandle<byte[]>(buffer, GCHandleType.Pinned))
			{
				var snapshot = (TransactionSnapshot)Marshal.PtrToStructure(handle.Value.AddrOfPinnedObject(), typeof(TransactionSnapshot));

				var execMsg = new ExecutionMessage
				{
					SecurityId = snapshot.SecurityId.ToSecurityId(),
					PortfolioName = snapshot.Portfolio,
					ServerTime = snapshot.LastChangeServerTime.To<DateTimeOffset>(),
					LocalTime = snapshot.LastChangeLocalTime.To<DateTimeOffset>(),

					OriginalTransactionId = snapshot.OriginalTransactionId,
					TransactionId = snapshot.TransactionId,

					BrokerCode = snapshot.BrokerCode,
					ClientCode = snapshot.ClientCode,

					Comment = snapshot.Comment,
					SystemComment = snapshot.SystemComment,

					Currency = snapshot.Currency == null ? (CurrencyTypes?)null : (CurrencyTypes)snapshot.Currency.Value,
					DepoName = snapshot.DepoName,
					Error = snapshot.Error.IsEmpty() ? null : new InvalidOperationException(snapshot.Error),

					ExpiryDate = snapshot.ExpiryDate?.To<DateTimeOffset>(),
					IsMarketMaker = snapshot.IsMarketMaker == null ? (bool?)null : (snapshot.IsMarketMaker.Value == 1),
					IsMargin = snapshot.IsMargin == null ? (bool?)null : (snapshot.IsMargin.Value == 1),
					Side = (Sides)snapshot.Side,
					OrderId = snapshot.OrderId,
					OrderStringId = snapshot.OrderStringId,
					OrderBoardId = snapshot.OrderBoardId,
					OrderPrice = snapshot.OrderPrice,
					OrderVolume = snapshot.OrderVolume,
					VisibleVolume = snapshot.VisibleVolume,
					OrderType = snapshot.OrderType == null ? (OrderTypes?)null : (OrderTypes)snapshot.OrderType.Value,
					OrderState = snapshot.OrderState == null ? (OrderStates?)null : (OrderStates)snapshot.OrderState.Value,
					OrderStatus = snapshot.OrderStatus,
					Balance = snapshot.Balance,
					UserOrderId = snapshot.UserOrderId,
					OriginSide = snapshot.OriginSide == null ? (Sides?)null : (Sides)snapshot.OriginSide.Value,
					Latency = snapshot.Latency == null ? (TimeSpan?)null : TimeSpan.FromTicks(snapshot.Latency.Value),
					PnL = snapshot.PnL,
					Position = snapshot.Position,
					Slippage = snapshot.Slippage,
					Commission = snapshot.Commission,
					TradePrice = snapshot.TradePrice,
					TradeVolume = snapshot.TradeVolume,
					TradeStatus = snapshot.TradeStatus,
					TradeId = snapshot.TradeId,
					TradeStringId = snapshot.TradeStringId,
					OpenInterest = snapshot.OpenInterest,
					IsSystem = snapshot.IsSystem == null ? (bool?)null : (snapshot.IsSystem.Value == 1),
					TimeInForce = snapshot.OrderTif == null ? (TimeInForce?)null : (TimeInForce)snapshot.OrderTif.Value,
				};
				
				return execMsg;
			}
		}

		long ISnapshotSerializer<long, ExecutionMessage>.GetKey(ExecutionMessage message)
		{
			return message.TransactionId == 0 ? message.OriginalTransactionId : message.TransactionId;
		}

		void ISnapshotSerializer<long, ExecutionMessage>.Update(ExecutionMessage message, ExecutionMessage changes)
		{
			if (!changes.BrokerCode.IsEmpty())
				message.BrokerCode = changes.BrokerCode;

			if (!changes.ClientCode.IsEmpty())
				message.ClientCode = changes.ClientCode;

			if (!changes.Comment.IsEmpty())
				message.Comment = changes.Comment;

			if (!changes.SystemComment.IsEmpty())
				message.SystemComment = changes.SystemComment;

			if (changes.Currency != null)
				message.Currency = changes.Currency;

			if (changes.Condition != null)
				message.Condition = changes.Condition.Clone();

			if (!changes.DepoName.IsEmpty())
				message.DepoName = changes.DepoName;

			if (changes.Error != null)
				message.Error = changes.Error;

			if (changes.ExpiryDate != null)
				message.ExpiryDate = changes.ExpiryDate;

			if (!changes.PortfolioName.IsEmpty())
				message.PortfolioName = changes.PortfolioName;

			if (changes.IsMarketMaker != null)
				message.IsMarketMaker = changes.IsMarketMaker;

			if (changes.HasOrderInfo)
				message.Side = changes.Side;

			if (changes.OrderId != null)
				message.OrderId = changes.OrderId;

			if (!changes.OrderBoardId.IsEmpty())
				message.OrderBoardId = changes.OrderBoardId;

			if (!changes.OrderStringId.IsEmpty())
				message.OrderStringId = changes.OrderStringId;

			if (changes.OrderType != null)
				message.OrderType = changes.OrderType;

			if (changes.OrderPrice != 0)
				message.OrderPrice = changes.OrderPrice;

			if (changes.OrderVolume != null)
				message.OrderVolume = changes.OrderVolume;

			if (changes.VisibleVolume != null)
				message.VisibleVolume = changes.VisibleVolume;

			if (changes.OrderState != null)
				message.OrderState = changes.OrderState;

			if (changes.OrderStatus != null)
				message.OrderStatus = changes.OrderStatus;

			if (changes.Balance != null)
				message.Balance = changes.Balance;

			if (!changes.UserOrderId.IsEmpty())
				message.UserOrderId = changes.UserOrderId;

			if (changes.OriginSide != null)
				message.OriginSide = changes.OriginSide;

			if (changes.Latency != null)
				message.Latency = changes.Latency;

			if (changes.PnL != null)
				message.PnL = changes.PnL;

			if (changes.Position != null)
				message.Position = changes.Position;

			if (changes.Slippage != null)
				message.Slippage = changes.Slippage;

			if (changes.Commission != null)
				message.Commission = changes.Commission;

			if (changes.TradePrice != null)
				message.TradePrice = changes.TradePrice;

			if (changes.TradeVolume != null)
				message.TradeVolume = changes.TradeVolume;

			if (changes.TradeStatus != null)
				message.TradeStatus = changes.TradeStatus;

			if (changes.TradeId != null)
				message.TradeId = changes.TradeId;

			if (!changes.TradeStringId.IsEmpty())
				message.TradeStringId = changes.TradeStringId;

			if (changes.OpenInterest != null)
				message.OpenInterest = changes.OpenInterest;

			if (changes.IsMargin != null)
				message.IsMargin = changes.IsMargin;

			if (changes.TimeInForce != null)
				message.TimeInForce = changes.TimeInForce;

			if (changes.OriginalTransactionId != 0)
				message.OriginalTransactionId = changes.OriginalTransactionId;

			if (changes.TransactionId != 0)
				message.TransactionId = changes.TransactionId;

			message.LocalTime = changes.LocalTime;
			message.ServerTime = changes.ServerTime;
		}

		DataType ISnapshotSerializer<long, ExecutionMessage>.DataType => DataType.Transactions;
	}
}