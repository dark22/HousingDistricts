using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using TShockAPI;
using System.IO.Streams;
using Terraria.DataStructures;
using Terraria.GameContent.Tile_Entities;
using Microsoft.Xna.Framework;

namespace HousingDistricts
{
	public delegate bool GetDataHandlerDelegate(GetDataHandlerArgs args);
	public class GetDataHandlerArgs : EventArgs
	{
		public TSPlayer Player { get; private set; }
		public MemoryStream Data { get; private set; }

		public Player TPlayer
		{
			get { return Player.TPlayer; }
		}

		public GetDataHandlerArgs(TSPlayer player, MemoryStream data)
		{
			Player = player;
			Data = data;
		}
	}
	public static class GetDataHandlers
	{
		static string EditHouse = "house.edit";
		static string TPHouse = "house.rod";
		private static Dictionary<PacketTypes, GetDataHandlerDelegate> GetDataHandlerDelegates;

		public static void InitGetDataHandler()
		{
			GetDataHandlerDelegates = new Dictionary<PacketTypes, GetDataHandlerDelegate>
			{
				{PacketTypes.Tile, HandleTile},
				{PacketTypes.TileSendSquare, HandleSendTileSquare},
				{PacketTypes.TileKill, HandlePlaceChest},
				{PacketTypes.LiquidSet, HandleLiquidSet},
				{PacketTypes.Teleport, HandleTeleport},
				{PacketTypes.PaintTile, HandlePaintTile},
				{PacketTypes.PaintWall, HandlePaintWall},
				{PacketTypes.PlaceObject, HandlePlaceObject},
				{PacketTypes.MassWireOperation, HandleMassWire},
				{PacketTypes.ChestGetContents, HandleChestGetContents},
				{PacketTypes.PlaceItemFrame, HandlePlaceItemFrame},
				{PacketTypes.HitSwitch, HandleHitSwitch},
				{PacketTypes.ChestItem, HandleChestItem}
			};
		}

		public static bool HandlerGetData(PacketTypes type, TSPlayer player, MemoryStream data)
		{
			GetDataHandlerDelegate handler;
			if (GetDataHandlerDelegates.TryGetValue(type, out handler))
			{
				try
				{
					return handler(new GetDataHandlerArgs(player, data));
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
				}
			}
			return false;
		}

		private static bool HandleSendTileSquare(GetDataHandlerArgs args)
		{
			var Start = DateTime.Now;

			short size = args.Data.ReadInt16();
			int tilex = args.Data.ReadInt16();
			int tiley = args.Data.ReadInt16();

			if (!args.Player.Group.HasPermission(EditHouse))
			{
				//lock (HousingDistricts.HPlayers)
				{
					var rect = new Rectangle(tilex, tiley, size, size);
					return House.HandlerAction((house) =>
					{
						if (HousingDistricts.Timeout(Start)) return false;
						if (house != null && house.HouseArea.Intersects(rect))
							if (!HTools.AllowedInHouse(args.Player.User, house))
							{
								args.Player.SendTileSquare(tilex, tiley);
								return true;
							}
						return false;
					});
				}
			}
			return false;
		}

		private static bool HandleTeleport(GetDataHandlerArgs args)
		{
			if (HConfigFile.Config.AllowRod || args.Player.Group.HasPermission(TPHouse))
				return false;

			var Start = DateTime.Now;

			var Flags = args.Data.ReadInt8();
			var ID = args.Data.ReadInt16();
			var X = args.Data.ReadSingle();
			var Y = args.Data.ReadSingle();

			if ((Flags & 2) != 2 && (Flags & 1) != 1 && !args.Player.Group.HasPermission(TPHouse))
			{
				//lock (HousingDistricts.HPlayers)
				{
					var rect = new Rectangle((int)(X / 16), (int)(Y / 16), 2, 4);
					return House.HandlerAction((house) =>
					{
						if (HousingDistricts.Timeout(Start)) return false;
						if (house != null && house.HouseArea.Intersects(rect))
							if (!HTools.CanVisitHouse(args.Player.User, house))
							{
								args.Player.SendErrorMessage(string.Format("You do not have permission to teleport into house '{0}'.", house.Name));
								args.Player.Teleport(args.TPlayer.position.X, args.TPlayer.position.Y);
								return true;
							}
						return false;
					});
				}
			}
			return false;
		}

		private static bool HandlePaintTile(GetDataHandlerArgs args)
		{
			var Start = DateTime.Now;

			var X = args.Data.ReadInt16();
			var Y = args.Data.ReadInt16();
			var T = args.Data.ReadInt8();

			if (!args.Player.Group.HasPermission(EditHouse))
			{
				//lock (HousingDistricts.HPlayers)
				{
					var rect = new Rectangle(X, Y, 1, 1);
					return House.HandlerAction((house) =>
					{
						if (HousingDistricts.Timeout(Start)) return false;
						if (house != null && house.HouseArea.Intersects(rect))
							if (!HTools.AllowedInHouse(args.Player.User, house))
							{
								args.Player.SendData(PacketTypes.PaintTile, "", X, Y, Main.tile[X, Y].color());
								return true;
							}
						return false;
					});
				}
			}
			return false;
		}

		private static bool HandlePaintWall(GetDataHandlerArgs args)
		{
			var Start = DateTime.Now;

			var X = args.Data.ReadInt16();
			var Y = args.Data.ReadInt16();
			var T = args.Data.ReadInt8();

			if (!args.Player.Group.HasPermission(EditHouse))
			{
				//lock (HousingDistricts.HPlayers)
				{
					var rect = new Rectangle(X, Y, 1, 1);
					return House.HandlerAction((house) =>
					{
						if (HousingDistricts.Timeout(Start)) return false;
						if (house != null && house.HouseArea.Intersects(rect))
							if (!HTools.AllowedInHouse(args.Player.User, house))
							{
								args.Player.SendData(PacketTypes.PaintWall, "", X, Y, Main.tile[X, Y].wallColor());
								return true;
							}
						return false;
					});
				}
			}
			return false;
		}

		private static bool HandleTile(GetDataHandlerArgs args)
		{
			var Start = DateTime.Now;

			args.Data.ReadInt8();
			int x = args.Data.ReadInt16();
			int y = args.Data.ReadInt16();
			//short tiletype = args.Data.ReadInt16();

			var player = HTools.GetPlayerByID(args.Player.Index);

			if (player.AwaitingHouseName)
			{
				if (HTools.InAreaHouseName(x, y) == null)
					args.Player.SendMessage("Tile is not in any House", Color.Yellow);
				else
					args.Player.SendMessage("House Name: " + HTools.InAreaHouseName(x, y), Color.Yellow);

				args.Player.SendTileSquare(x, y);
				player.AwaitingHouseName = false;
				return true;
			}

			if (args.Player.AwaitingTempPoint > 0)
			{
				args.Player.TempPoints[args.Player.AwaitingTempPoint - 1].X = x;
				args.Player.TempPoints[args.Player.AwaitingTempPoint - 1].Y = y;

				if (args.Player.AwaitingTempPoint == 1)
					args.Player.SendMessage("Top-left corner of protection area has been set!", Color.Yellow);

				if (args.Player.AwaitingTempPoint == 2)
					args.Player.SendMessage("Bottom-right corner of protection area has been set!", Color.Yellow);

				args.Player.SendTileSquare(x, y);
				args.Player.AwaitingTempPoint = 0;
				return true;
			}
			if (!args.Player.Group.HasPermission(EditHouse))
			{
				//lock (HousingDistricts.HPlayers)
				{
					var rect = new Rectangle(x, y, 1, 1);
					return House.HandlerAction((house) =>
					{
						if (HousingDistricts.Timeout(Start)) return false;
						if (house != null && house.HouseArea.Intersects(rect))
							if (!HTools.AllowedInHouse(args.Player.User, house))
							{
								args.Player.SendTileSquare(x, y);
								return true;
							}
						return false;
					});
				}
			}
			return false;
		}

		private static bool HandleMassWire(GetDataHandlerArgs args)
		{
			var Start = DateTime.Now;

			int x1 = args.Data.ReadInt16();
			int y1 = args.Data.ReadInt16();
			int x2 = args.Data.ReadInt16();
			int y2 = args.Data.ReadInt16();

			var player = HTools.GetPlayerByID(args.Player.Index);;

			if (args.Player.AwaitingTempPoint > 0)
			{
				args.Player.TempPoints[0].X = x1;
				args.Player.TempPoints[0].Y = y1;
				args.Player.TempPoints[1].X = x2;
				args.Player.TempPoints[1].Y = y2;

				args.Player.SendMessage("Protection corners have been set!", Color.Yellow);
				args.Player.AwaitingTempPoint = 0;
				return true;
			}
			if (!args.Player.Group.HasPermission(EditHouse))
			{
				Rectangle A = new Rectangle(Math.Min(x1, x2), args.TPlayer.direction != 1 ? y1 : y2, Math.Abs(x2 - x1) + 1, 1);
				Rectangle B = new Rectangle(args.TPlayer.direction != 1 ? x2 : x1, Math.Min(y1, y2), 1, Math.Abs(y2 - y1) + 1);

				//lock (HousingDistricts.HPlayers)
				{
					return House.HandlerAction((house) =>
					{
						if (HousingDistricts.Timeout(Start)) return false;
						if (house != null && (house.HouseArea.Intersects(A) || house.HouseArea.Intersects(B)))
							if (!HTools.AllowedInHouse(args.Player.User, house))
								return true;
						return false;
					});
				}
			}
			return false;
		}

		private static bool HandlePlaceObject(GetDataHandlerArgs args)
		{
			var Start = DateTime.Now;

			int x = args.Data.ReadInt16();
			int y = args.Data.ReadInt16();
			//short tiletype = args.Data.ReadInt16();

			var player = HTools.GetPlayerByID(args.Player.Index);

			if (player.AwaitingHouseName)
			{
				if (HTools.InAreaHouseName(x, y) == null)
					args.Player.SendMessage("Tile is not in any House", Color.Yellow);
				else
					args.Player.SendMessage("House Name: " + HTools.InAreaHouseName(x, y), Color.Yellow);

				args.Player.SendTileSquare(x, y);
				player.AwaitingHouseName = false;
				return true;
			}

			if (args.Player.AwaitingTempPoint > 0)
			{
				args.Player.TempPoints[args.Player.AwaitingTempPoint - 1].X = x;
				args.Player.TempPoints[args.Player.AwaitingTempPoint - 1].Y = y;

				if (args.Player.AwaitingTempPoint == 1)
					args.Player.SendMessage("Top-left corner of protection area has been set!", Color.Yellow);

				if (args.Player.AwaitingTempPoint == 2)
					args.Player.SendMessage("Bottom-right corner of protection area has been set!", Color.Yellow);

				args.Player.SendTileSquare(x, y);
				args.Player.AwaitingTempPoint = 0;
				return true;
			}
			if (!args.Player.Group.HasPermission(EditHouse))
			{
				//lock (HousingDistricts.HPlayers)
				{
					var rect = new Rectangle(x, y, 1, 1);
					return House.HandlerAction((house) =>
					{
						if (HousingDistricts.Timeout(Start)) return false;
						if (house != null && house.HouseArea.Intersects(rect))
							if (!HTools.AllowedInHouse(args.Player.User, house))
							{
								args.Player.SendTileSquare(x, y);
								return true;
							}
						return false;
					});
				}
			}
			return false;
		}

		private static bool HandleLiquidSet(GetDataHandlerArgs args)
		{
			var Start = DateTime.Now;

			int X = args.Data.ReadInt16();
			int Y = args.Data.ReadInt16();

			if (!args.Player.Group.HasPermission(EditHouse))
			{
				//lock (HousingDistricts.HPlayers)
				{
					var rect = new Rectangle(X, Y, 1, 1);
					return House.HandlerAction((house) =>
					{
						if (HousingDistricts.Timeout(Start)) return false;
						if (house != null && house.HouseArea.Intersects(rect))
							if (!HTools.AllowedInHouse(args.Player.User, house))
							{
								args.Player.SendTileSquare(X, Y);
								return true;
							}
						return false;
					});
				}
			}
			return false;
		}

		private static bool HandlePlaceChest(GetDataHandlerArgs args)
		{
			var Start = DateTime.Now;
			int action = args.Data.ReadByte();
			int X = args.Data.ReadInt16();
			int Y = args.Data.ReadInt16();

			int tilex = Math.Abs(X);
			int tiley = Math.Abs(Y);
			var player = HTools.GetPlayerByID(args.Player.Index);

			if (!args.Player.Group.HasPermission(EditHouse))
			{
				//lock (HousingDistricts.HPlayers)
				{
					var rect = new Rectangle(X, Y, 1, 1);
					return House.HandlerAction((house) =>
					{
						if (HousingDistricts.Timeout(Start)) return false;
						if (house != null && house.HouseArea.Intersects(rect))
							if (!HTools.OwnsHouse(args.Player.User, house))
							{
								args.Player.SendTileSquare(X, Y);
								return true;
							}

						if (HTools.InAreaHouseName(X, Y) == null)
						{
							if (action == 0 || action == 2)
							{
								args.Player.SendErrorMessage("You must place this in your house.");
								args.Player.SendTileSquare(tilex, tiley, 3);
								return true;
							}
						}
						if (house != null && house.HouseArea.Intersects(rect))
						{
							if (!HTools.AllowedInHouse(args.Player.User, house))
							{
								args.Player.SendTileSquare(tilex, tiley);
								return true;
							}
							if (HTools.AllowedInHouse(args.Player.User, house))
							{
								if (action == 0 || action == 2)
								{
									int maxChests = 1000;
									int iCount = 0;
									for (int iChest = 0; iChest < maxChests; iChest++)
									{
										if (Main.chest[iChest] != null && house.HouseArea.Intersects(new Rectangle(Main.chest[iChest].x, Main.chest[iChest].y, 1, 1)))
										{
											iCount++;
											if (iCount >= HousingDistricts.HConfig.MaxChests)
											{
												args.Player.SendErrorMessage("Houses must contain {0} or less.", HousingDistricts.HConfig.MaxChests);
												args.Player.SendTileSquare(X, Y, 3);
												return true;
											}
										}
									}
								}
							}
						}
						return false;
					});
				}
			}
			return false;
		}

		private static bool HandleChestGetContents(GetDataHandlerArgs args)
		{
			var Start = DateTime.Now;
			int x = args.Data.ReadInt16();
			int y = args.Data.ReadInt16();

			int tilex = Math.Abs(x);
			int tiley = Math.Abs(y);

			if (!args.Player.Group.HasPermission(EditHouse))
			{
				if (HousingDistricts.HConfig.ProtectContents)
				{
					lock (HousingDistricts.HPlayers)
					{
						var I = HousingDistricts.Houses.Count;
						for (int i = 0; i < I; i++)
						{
							if (HousingDistricts.Timeout(Start)) return false;
							var house = HousingDistricts.Houses[i];
							if (house != null && house.HouseArea.Intersects(new Rectangle(tilex, tiley, 1, 1)))
							{
								if (!HTools.AllowedInHouse(args.Player.User, house))
								{
									args.Player.SendErrorMessage("This chest is protected.");
									return true;
								}
							}
						}
					}
				}
			}
			return false;
		}

		private static bool HandlePlaceItemFrame(GetDataHandlerArgs args)
		{
			var Start = DateTime.Now;
			var X = args.Data.ReadInt16();
			var Y = args.Data.ReadInt16();
			var itemID = args.Data.ReadInt16();
			var prefix = args.Data.ReadInt8();
			var stack = args.Data.ReadInt16();
			var itemFrame = (TEItemFrame)TileEntity.ByID[TEItemFrame.Find(X, Y)];

			int tilex = Math.Abs(X);
			int tiley = Math.Abs(Y);

			if (!args.Player.Group.HasPermission(EditHouse))
			{
				if (HousingDistricts.HConfig.ProtectContents)
				{
					//lock (HousingDistricts.HPlayers)
					{
						var rect = new Rectangle(X, Y, 1, 1);
						var I = HousingDistricts.Houses.Count;
						for (int i = 0; i < I; i++)
						{
							if (HousingDistricts.Timeout(Start)) return false;
							var house = HousingDistricts.Houses[i];
							if (house != null && house.HouseArea.Intersects(rect))
							{
								if (!HTools.AllowedInHouse(args.Player.User, house))
								{
									args.Player.SendErrorMessage("The Item Frame is protected in this house.");
									return true;
								}
							}
						}
					}
				}
			}
			return false;
		}

		private static bool HandleHitSwitch(GetDataHandlerArgs args)
		{
			var Start = DateTime.Now;
			var X = args.Data.ReadInt16();
			var Y = args.Data.ReadInt16();

			int tilex = Math.Abs(X);
			int tiley = Math.Abs(Y);

			if (!args.Player.Group.HasPermission(EditHouse))
			{
				if (HousingDistricts.HConfig.ProtectContents)
				{
					var rect = new Rectangle(X, Y, 1, 1);
					var I = HousingDistricts.Houses.Count;
					for (int i = 0; i < I; i++)
					{
						if (HousingDistricts.Timeout(Start)) return false;
						var house = HousingDistricts.Houses[i];
						if (house != null && house.HouseArea.Intersects(rect))
						{
							if (!HTools.AllowedInHouse(args.Player.User, house))
							{
								args.Player.SendErrorMessage("The Switch is protected in this house.");
								return true;
							}
						}
					}
				}
			}
			return false;
		}

		private static bool HandleChestItem(GetDataHandlerArgs args)
		{
			var id = args.Data.ReadInt16();
			var slot = args.Data.ReadByte();
			var stack = args.Data.ReadInt16();
			var prefix = args.Data.ReadByte();
			var itemid = args.Data.ReadInt16();

			int count = 0;
			Chest iChest = Main.chest[id];

			foreach (Item item in iChest.item)
			{
				if (item.netID > 0)
					count++;
			}

			int X = iChest.x;
			int Y = iChest.y;

			if (HTools.InAreaHouseName(X, Y) == null)
			{
				if (itemid != 0)
				{
					args.Player.SendErrorMessage("You can only take items from World Chests.");
					return true;
				}
			}

			return false;
		}
	}
}
