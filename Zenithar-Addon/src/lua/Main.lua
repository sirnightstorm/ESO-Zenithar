Zen = {
	name = "Zenithar",
	displayName = "Zenithar",
	processors = {},
	lastEvent = nil,
	prefs = {
		guildId = 767808,
	},
	data = {
		processed = 0,
		language = ""
	},
}

local prefsDefaults = {
	disableWarnings = false,
	guildId = 767808,
	loggingEnabled = true,
	showWindow = true
}

local dataDefaults = {
	processed = 0,
}

Zen.logger = LibDebugLogger and LibDebugLogger(Zen.name)

function Zen.Log(...)
	if Zen.logger and Zen.prefs and Zen.prefs["loggingEnabled"] then
		Zen.logger:Info(string.format(...))
	end
end

function Zen.LogDebug(...)
	if Zen.logger and Zen.prefs and Zen.prefs["loggingEnabled"] then
		Zen.logger:Debug(string.format(...))
	end
end

function Zen.LogWarning(...)
	if Zen.logger and Zen.prefs and Zen.prefs["loggingEnabled"] then
		Zen.logger:Warn(string.format(...))
	end
end

function Zen:GetGuildData(guildId)
	if not self.data["guild:"..guildId] then
		Zen.Log("Resetting guild '%s'", GetGuildName(guildId))
		self.data["guild:"..guildId] = {
			lastEvent = {},
			users = {},
			items = {},
			txns = {},
			ids = {},
		}
	end
	return self.data["guild:"..guildId]
end

function Zen:MonitorGuild()
	EVENT_MANAGER:UnregisterForUpdate(self.name)
	local guildId = self.prefs.guildId
	if guildId == nil or guildId == false or guildId == 0 then return end

	LibHistoire:OnReady(function(lib)
		self:ProcessItems(lib, guildId, GUILD_HISTORY_EVENT_CATEGORY_BANKED_ITEM)
		self:ProcessItems(lib, guildId, GUILD_HISTORY_EVENT_CATEGORY_BANKED_CURRENCY)
	end)
end

function Zen:ProcessItems(lib, guildId, eventCategory)
	if not self.processors[guildId] then self.processors[guildId] = {} end
	if not self.processors[guildId][eventCategory] then
		self.processors[guildId][eventCategory] = lib:CreateGuildHistoryProcessor(guildId, eventCategory, "Zenithar")
	end
	local processor = self.processors[guildId][eventCategory]

	if not processor then
		return -- the processor could not be created
	end

	processor:Stop()

	processor:SetReceiveMissedEventsOutsideIterationRange(true)

	local guildData = self:GetGuildData(guildId)
	local started = processor:StartStreaming(guildData.lastEvent[eventCategory], function(event)
		self:ProcessEvent(event)
		guildData.lastEvent[eventCategory] = event:GetEventInfo().eventId
	end)

	if not started then
		Zen.LogWarning("Failed to start processor for category %d", eventCategory)
	end
end

local function getRankIndex(guildId, userName)
	local _, _, rankIndex, _, _ = GetGuildMemberInfo(guildId, GetGuildMemberIndexFromDisplayName(guildId, userName))
	return rankIndex
end

function Zen:GetUser(user, timestampS)
	local guildId = self.prefs.guildId
	local guildData = self:GetGuildData(guildId)

	if guildData.users[user] == nil then
		guildData.users[user] = {
			id = self:GetNextId('user'),
			rankIndex = getRankIndex(guildId, user), -- FIXME: Need to re-run this every load?
			--initialScan = timestampS,
		}
		Zen.Log("Created user '%s' id %d", user, guildData.users[user].id)
	end

	return guildData.users[user]
end

function Zen:GetUserName(userId)
	local guildId = self.prefs.guildId
	local guildData = self:GetGuildData(guildId)

	for name, data in pairs(guildData.users) do
		if data.id == userId then
			return name
		end
	end
	return nil -- not found
end

function Zen:GetItem(itemLink)
	local guildId = self.prefs.guildId
	local guildData = self:GetGuildData(guildId)

	local items = guildData.items
	local name = zo_strformat("<<!AC:1>>", GetItemLinkName(itemLink))
	--local icon = GetItemLinkInfo(itemLink)

	if items[itemLink] == nil then
		items[itemLink] = {
			id = self:GetNextId('item'),
			name = name,
			--icon = icon,
		}
		Zen.Log("Created item '%s' id %d", name, guildData.items[itemLink].id)
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

function Zen:StoreTransaction(userObj, txnType, info, event, gold)
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
		gold = math.floor(gold + 0.5)
	elseif txnType == "-item" then
		itemId = self:GetItem(info.itemLink).id
		qty = -qty
		gold = -math.floor(gold + 0.5)
	elseif txnType == "+gold" then
		gold = info.amount
		qty = nil
	elseif txnType == "-gold" then
		gold = -info.amount
		qty = nil
	end

	--if itemId ~= nil then
	--	Zen.Log("Store item transaction %d for %s", eventId, userName)
	--	data = string.format("%s~%d~%d~%s~%d", userId, timestampS, qty, itemId, price)
	--else
	--	Zen.Log("Store gold transaction %d for %s", eventId, userName)
	--	data = string.format("%s~%d~%d", userId, timestampS, qty)
	--end

	self:GetGuildData(self.prefs.guildId).txns[eventId] = {
		ts = timestampS,
		user = userId,
		gold = gold,
		qty = qty,
		item = itemId
	}

	self.lastEvent = GetFrameTimeMilliseconds()
	self:UpdateWindow()
	self:PlaySound()
end

function Zen:GetNextId(namespace)
	local guildData = self:GetGuildData(self.prefs.guildId)
	guildData.ids[namespace] = (guildData.ids[namespace] or 0) + 1
	return guildData.ids[namespace]
end

function Zen:ClearData()
	Zen.Log("Clearing down data for guild '%s'", GetGuildName(self.prefs.guildId))
	local guildData = self:GetGuildData(self.prefs.guildId)
	guildData.users = {}
	guildData.items = {}
	guildData.txns = {}
	guildData.ids = {}
	Zen.data.processed = 0
end

function Zen:PlaySound()
	if not self.soundQueue then
		self.soundQueue = ZO_QueuedSoundPlayer:New(1000)
	end
	if not self.soundQueue:IsPlaying() then
		self.soundQueue:PlaySound(SOUNDS.FENCE_ITEM_LAUNDERED, 1000)
	end
	--/script PlaySound(SOUNDS.FENCE_ITEM_LAUNDERED) -- GUILD_HERALDRY_APPLIED -- ALLIANCE_POINT_TRANSACT
	-- CHAMPION_STAR_STAGE_UP -- EVENT_TICKET_TRANSACT -- GROUP_FINDER_REFRESH_SEARCH
end

function Zen.Cmd(txt)
	if txt == "reset" then
		Zen.data["guild:"..Zen.prefs.guildId] = nil
		Zen.Log("Cleared data for guild '%s'", GetGuildName(Zen.prefs.guildId))
		Zen:MonitorGuild()
	else
		-- TESTING!
		Zen.lastEvent = GetFrameTimeMilliseconds()
		Zen:UpdateWindow()
	end
end

function Zen.OnAddOnLoaded(_, addon)
	if addon ~= Zen.name then return end

	Zen.prefs = ZO_SavedVars:NewAccountWide("Zenithar_prefs", 1, nil, prefsDefaults)
	Zen.data = ZO_SavedVars:NewAccountWide("Zenithar_data", 1, nil, dataDefaults)

	if Zen.data.processed == 1 then
		Zen:ClearData()
	end

	Zen.data.language = Zen.GetCurrentLanguage()

	--Zen:Menu()
	SLASH_COMMANDS["/zenithar"] = Zen.Cmd
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

	Zen:CreateWindow()
end

EVENT_MANAGER:RegisterForEvent(Zen.name, EVENT_ADD_ON_LOADED, Zen.OnAddOnLoaded)