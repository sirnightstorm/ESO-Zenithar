Zen = {
	name = "Zenithar",
	displayName = "Zenithar",
	processors = {},
	prefs = {
		guildId = 767808,
	},
	data = {
		processed = 0,
		guilds = {},
	},
}

local prefsDefaults = {
	disableWarnings = false,
	guildId = 767808,
	loggingEnabled = true
}

local dataDefaults = {
	processed = 0,
	guilds = {}
}

local logger = LibDebugLogger and LibDebugLogger(Zen.name)

local _events = {}

function Zen.Log(...)
	if logger and Zen.prefs and Zen.prefs["loggingEnabled"] then
		logger:Debug(string.format(...))
	end
end

function Zen.LogWarning(...)
	if logger and Zen.prefs and Zen.prefs["loggingEnabled"] then
		logger:Warn(string.format(...))
	end
end

function Zen:ResetGuild(guildId)
	Zen.Log("Resetting guild '%s'", GetGuildName(guildId))
	self.data.guilds[guildId] = {
		lastEvent = {},
		users = {},
		items = {},
		txns = {},
		ids = {},
	}
end

function Zen:MonitorGuild()
	EVENT_MANAGER:UnregisterForUpdate(self.name)
	if self.prefs.guildId == nil then return end
	local guildId = self.prefs.guildId
	if guildId == false or guildId == 0 then return end

	if not self.data.guilds[guildId] then self:ResetGuild(guildId) end

	LibHistoire:OnReady(function(lib)
		self:ProcessItems(self, lib, guildId, GUILD_HISTORY_EVENT_CATEGORY_BANKED_ITEM)
		self:ProcessItems(self, lib, guildId, GUILD_HISTORY_EVENT_CATEGORY_BANKED_CURRENCY)
	end)
end

function Zen:ProcessItems(self, lib, guildId, eventCategory)
	local processor = self:GetProcessor(lib, guildId, eventCategory) --lib:CreateGuildHistoryProcessor(guildId, eventCategory, "ExcessiveWithdrawals")
	if not processor then
		return -- the processor could not be created
	end

	processor:Stop()

	local started = processor:StartStreaming(self.data.guilds[guildId].lastEvent[eventCategory], function(event)
		self:ProcessEvent(event)
		self.data.guilds[guildId].lastEvent[eventCategory] = event:GetEventInfo().eventId
	end)

	if not started then
		Zen.LogWarning("Failed to start processor for category %d", eventCategory)
	end
end

function Zen:GetProcessor(lib, guildId, eventCategory)
	if not self.processors[guildId] then self.processors[guildId] = {} end
	if not self.processors[guildId][eventCategory] then
		self.processors[guildId][eventCategory] = lib:CreateGuildHistoryProcessor(guildId, eventCategory, "Zenithar")
	end
	return self.processors[guildId][eventCategory]
end

local function getRankIndex(guildId, userName)
	local _, _, rankIndex, _, _ = GetGuildMemberInfo(guildId, GetGuildMemberIndexFromDisplayName(guildId, userName))
	return rankIndex
end

function Zen:GetUser(user, timestampS)
	local guildId = self.prefs.guildId

	if self.data.guilds[guildId].users[user] == nil then
		self.data.guilds[guildId].users[user] = {
			id = self:GetNextId('user'),
			initialScan = timestampS,
			rankIndex = getRankIndex(guildId, user), -- FIXME: Need to re-run this every load?
		}
		Zen.Log("Created user '%s' id %d", user, self.data.guilds[guildId].users[user].id)
	end

	return self.data.guilds[guildId].users[user]
end

function Zen:GetUserName(userId)
	local guildId = self.prefs.guildId
	for name, data in pairs(self.data.guilds[guildId].users) do
		if data.id == userId then
			return name
		end
	end
	return nil -- not found
end

function Zen:GetItem(itemLink)
	local guildId = self.prefs.guildId
	local items = self.data.guilds[guildId].items
	local name = GetItemLinkName(itemLink)
	local icon = GetItemLinkInfo(itemLink)

	if items[itemLink] == nil then
		items[itemLink] = {
			id = self:GetNextId('item'),
			name = name,
			icon = icon,
		}
		Zen.Log("Created item '%s' id %d", name, self.data.guilds[guildId].items[itemLink].id)
	end

	return items[itemLink]
end

function Zen:ProcessEvent(event)
	local info = event:GetEventInfo()
	local user = "@"..info.displayName
	local userObj = self:GetUser(user, event:GetEventTimestampS())

	if event:GetEventCategory() == GUILD_HISTORY_EVENT_CATEGORY_BANKED_CURRENCY then
		self:ProcessCashEvent(userObj, event, info)
	else
		self:ProcessItemEvent(userObj, event, info)
	end
end

function Zen:ProcessItemEvent(userObj, event, info)
	--local eventTime = event:GetEventTimestampS()
	--local category = event:GetEventCategory()
	local type = event:GetEventType()
	local qty = info.quantity

	if qty < 0 then qty = qty * -1 end
	local price = (info.quantity ~= 0) and self:GetPrice(info.itemLink) * qty or 0

	if type == GUILD_HISTORY_BANKED_ITEM_EVENT_ADDED then
		if self.prefs.logging then d(string.format("%s - %s: +%d %s (worth %d)", self.displayName, userObj.userName, qty, info.itemLink, price)) end
		self:StoreTransaction(userObj, "+item", info, event, price)
	elseif type == GUILD_HISTORY_BANKED_ITEM_EVENT_REMOVED then
		if self.prefs.logging then d(string.format("%s - %s: -%d %s (worth %d)", self.displayName, userObj.userName, qty, info.itemLink, price)) end
		self:StoreTransaction(userObj, "-item", info, event, price)
	end

	if info.quantity < 0 then userObj.ignore = false end
end

function Zen:ProcessCashEvent(userObj, event, info)
	local type = event:GetEventType()
	local amount = info.amount

	if amount then
		if type == GUILD_HISTORY_BANKED_CURRENCY_EVENT_DEPOSITED then
			if self.prefs.logging then d(string.format("%s - %s: +%d gold", self.displayName, userObj.userName, amount)) end
			self:StoreTransaction(userObj, "+gold", info, event)
		elseif type == GUILD_HISTORY_BANKED_CURRENCY_EVENT_WITHDRAWN then
			if self.prefs.logging then d(string.format("%s - %s: -%d gold", self.displayName, userObj.userName, amount)) end
			self:StoreTransaction(userObj, "-gold", info, event)
		end
	else
		Zen.logWarning("Stored event for nil amount event")
		ExcessiveWithdrawals.event = event
	end
end

function Zen:StoreTransaction(userObj, txnType, info, event, price)
	-- userObj.userName, info.eventId, event:GetEventTimestampS(), qty, info.itemLink, price
	--local guildName = GetGuildName(self.prefs.guildId)
	--local userName = userObj.userName:sub(2)
	local eventId = info.eventId
	local timestampS = event:GetEventTimestampS()
	local itemId
	local qty = info.quantity
	local userId = userObj.id
	local userName = self:GetUserName(userObj.id)

	if txnType == "+item" then
		itemId = self:GetItem(info.itemLink).id
	elseif txnType == "-item" then
		itemId = self:GetItem(info.itemLink).id
		qty = -qty
	elseif txnType == "+gold" then
		qty = info.amount
	elseif txnType == "-gold" then
		qty = -info.amount
	end

	--if not Zen.data.guilds[guildName] then
	--	Zen.data.guilds[guildName] = {}
	--	Zen.data.guilds[guildName]['_lastEvent'] = {}
	--	d(self.displayName .. ": create guild")
	--end
	local data
	if itemId ~= nil then
		Zen.Log("Store item transaction %d for %s", eventId, userName)
		data = string.format("%s~%d~%d~%s~%d", userId, timestampS, qty, itemId, price)
	else
		Zen.Log("Store gold transaction %d for %s", eventId, userName)
		data = string.format("%s~%d~%d", userId, timestampS, qty)
	end

	Zen.data.guilds[self.prefs.guildId].txns[eventId] = data
end

function Zen:GetNextId(namespace)
	self.data.guilds[self.prefs.guildId].ids[namespace] = (self.data.guilds[self.prefs.guildId].ids[namespace] or 0) + 1
	return self.data.guilds[self.prefs.guildId].ids[namespace]
end

function Zen:ClearData()
	Zen.Log("Clearing down data for guild '%s'", GetGuildName(Zen.prefs.guildId))
	self.data.guilds[self.prefs.guildId].users = {}
	self.data.guilds[self.prefs.guildId].items = {}
	self.data.guilds[self.prefs.guildId].txns = {}
	self.data.guilds[self.prefs.guildId].ids = {}
	Zen.data.processed = 0
end


function Zen.OnAddOnLoaded(_, addon)
	if addon ~= Zen.name then return end

	Zen.prefs = ZO_SavedVars:NewAccountWide("Zenithar_prefs", 1, nil, prefsDefaults)
	Zen.data = ZO_SavedVars:NewAccountWide("Zenithar_data", 1, nil, dataDefaults)

	if Zen.data.processed == 1 then
		Zen:ClearData()
	end

	--Zen:Menu()
	--SLASH_COMMANDS["/zenithar"] = Zenithar.Cmd
	if Zen.prefs.disableWarnings == false then
		if MasterMerchant == nil and TamrielTradeCentrePrice == nil then
			CHAT_SYSTEM:AddMessage(Zen.displayName .. " -- \n|cFF0000ERROR: Master Merchant and Tamriel Trade Centre addons were not found.|r\nDefault system prices will be used instead!")
		elseif MasterMerchant == nil then
			CHAT_SYSTEM:AddMessage(Zen.displayName .. " -- \n|cFF0000Warning: Master Merchant addon was not found.|r\nPrices used for calculations may not reflect your current market value!")
		end
	end
	EVENT_MANAGER:UnregisterForEvent(Zen.name, EVENT_ADD_ON_LOADED)
	--EVENT_MANAGER:RegisterForUpdate(Zenithar.name, 30000, function() Zenithar:MonitorGuild() end)
	if Zen.prefs.guildId then
		Zen.Log("Starting to monitor guild '%s'", GetGuildName(Zen.prefs.guildId))
		Zen:MonitorGuild()
	end
	--Zen.AdjustContextMenus()

	--Zen.window:Init()
	--Zen.userWindow:Init()

end

EVENT_MANAGER:RegisterForEvent(Zen.name, EVENT_ADD_ON_LOADED, Zen.OnAddOnLoaded)