using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TShockAPI;
using TShockAPI.DB;
using Microsoft.Xna.Framework;

namespace HousingDistricts
{
	class HTools
	{
		public static House GetHouseByName(string name)
		{
			if (String.IsNullOrEmpty(name))
				return null;

			var I = HousingDistricts.Houses.Count;
			for (int i = 0; i < I; i++)
			{
				var house = HousingDistricts.Houses[i];
				if (house.Name == name)
					return house;
			}
			return null;
		}

		public static void BroadcastToHouse(House house, string text, string playername)
		{
			var I = HousingDistricts.HPlayers.Count;
			for (int i = 0; i < I; i++)
			{
				var player = HousingDistricts.HPlayers[i];
				if (house.HouseArea.Intersects(new Rectangle(player.TSPlayer.TileX, player.TSPlayer.TileY, 1, 1)))
					player.TSPlayer.SendMessage("<House> <" + playername + ">: " + text, Color.LightSkyBlue);
			}
		}

		public static string InAreaHouseName(int x, int y)
		{
			var I = HousingDistricts.Houses.Count;
			for (int i = 0; i < I; i++)
			{
				var house = HousingDistricts.Houses[i];
				if (x >= house.HouseArea.Left && x < house.HouseArea.Right &&
					y >= house.HouseArea.Top && y < house.HouseArea.Bottom)
					return house.Name;
			}
			return null;
		}

		public static void BroadcastToHouseOwners(string housename, string text)
		{
			BroadcastToHouseOwners(HTools.GetHouseByName(housename), text);
		}

		public static void BroadcastToHouseOwners(House house, string text)
		{
			foreach (TSPlayer player in TShock.Players)
				if (player != null && player.User != null && house.Owners.Contains(player.User.ID.ToString()))
						player.SendMessage(text, Color.LightSeaGreen);
		}

		public static bool OwnsHouse(User user, House house)
		{
			bool isAdmin = false;
			try
			{
				isAdmin = TShock.Groups.GetGroupByName(user.Group).HasPermission("house.root");
			}
			catch { }
			if (user != null && house != null)
			{
				try
				{
					if (house.Owners[0] == user.ID.ToString() || isAdmin)
						return true;
					else
						return false;
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
					return false;
				}
			}
			return false;
		}

		public static bool AllowedInHouse(User user, House house)
		{
			bool isAdmin = false;
			try
			{
				isAdmin = TShock.Groups.GetGroupByName(user.Group).HasPermission("house.root");
			}
			catch { }
			if (user != null && house != null)
			{
				try
				{
					if (house.Owners.Contains(user.ID.ToString()) || isAdmin) return true;
					else return false;
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
					return false;
				}
			}
			return false;
		}

		public static bool CanVisitHouse(string UserID, House house)
		{
			return (!String.IsNullOrEmpty(UserID) && UserID != "0") && (house.Visitors.Contains(UserID) || house.Owners.Contains(UserID)); 
		}

		public static bool CanVisitHouse(User U, House house)
		{
			return (U != null && U.ID != 0) && (house.Visitors.Contains(U.ID.ToString()) || house.Owners.Contains(U.ID.ToString()));
		}

		public static House HouseAtPosition(int X, int Y)
		{
			var H = HousingDistricts.Houses.Count;
			for (int h = 0; h < H; h++)
			{
				var house = HousingDistricts.Houses[h];
				if (house.HouseArea.Intersects(new Rectangle(X, Y, 1, 1)))
				{
					return house;
				}
			}
			return null;
		}

		public static HPlayer GetPlayerByID(int id)
		{
			var I = HousingDistricts.HPlayers.Count;
			for (int i = 0; i < I; i++)
			{
				var player = HousingDistricts.HPlayers[i];
				if (player.Index == id) return player;
			}

			return new HPlayer();
		}

		public static int MaxSize(TSPlayer ply)
		{
			var I = ply.Group.permissions.Count;
			for (int i = 0; i < I; i++)
			{
				var perm = ply.Group.permissions[i];
				Match Match = Regex.Match(perm, @"^house\.size\.(\d{1,9})$");
				if (Match.Success)
					return Convert.ToInt32(Match.Groups[1].Value);
			}
			return HConfigFile.Config.MaxHouseSize;
		}

		public static int MaxCount(TSPlayer ply)
		{
			var I = ply.Group.permissions.Count;
			for (int i = 0; i < I; i++)
			{
				var perm = ply.Group.permissions[i];
				Match Match = Regex.Match(perm, @"^house\.count\.(\d{1,9})$");
				if (Match.Success)
					return Convert.ToInt32(Match.Groups[1].Value);
			}
			return HConfigFile.Config.MaxHousesByUsername;
		}

		public static bool hasMaxHouses(User user)
		{
			List<int> userOwnedHouses = new List<int>();
			House house = null;
			var H = HousingDistricts.Houses.Count;
			for (int h = 0; h < H; h++)
			{
				house = HousingDistricts.Houses[h];
				if (house != null)
				{
					for (int i = 0; i < house.Owners.Count; i++)
					{
						User owner = HTools.GetUserIDHouse(house.Owners[i]);
						if (owner != null && user.Name == owner.Name)
							userOwnedHouses.Add(house.ID);
					}
				}
			}

			int MaxHouses = HConfigFile.Config.MaxHousesByUsername;
			TShockAPI.Group grp = TShock.Utils.GetGroup(user.Group);
			var I = grp.permissions.Count;
			for (int i = 0; i < I; i++)
			{
				var perm = grp.permissions[i];
				Match Match = Regex.Match(perm, @"^house\.count\.(\d{1,9})$");
				if (Match.Success)
				{
					MaxHouses += Convert.ToInt32(Match.Groups[1].Value);
				}
			}
			if (userOwnedHouses.Count > MaxHouses)
				return true;
			return false;
		}

		public static User GetUserIDHouse(string UserID)
		{
			if (!string.IsNullOrEmpty(UserID))
			{
				return TShock.Users.GetUserByID(Convert.ToInt32(UserID));
			}
			return null;
		}
	}
}
