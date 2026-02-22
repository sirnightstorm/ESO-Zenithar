function Zen:GetPrice(item)
	local price = 0
	if item == "gold" then
		price = 1
	else
		if MasterMerchant ~= nil then
			local itemStats = MasterMerchant:itemStats(item, true)
			price = itemStats.avgPrice
		end
		if TamrielTradeCentrePrice ~= nil and (MasterMerchant == nil or price == nil) then
			local priceInfo = TamrielTradeCentrePrice:GetPriceInfo(item)
			if priceInfo ~= nil then
				if priceInfo.SuggestedPrice ~= nil then
					price = priceInfo.SuggestedPrice
				elseif priceInfo.Avg ~= nil then
					price = priceInfo.Avg
				else
					price = 0
				end
			else
				price = 0
			end
		end
		if price == nil or price == 0 then
			_, price, _, _, _ = GetItemLinkInfo(item)
		else
			price = zo_round(price * 100)
			price = price / 100
		end
	end
	return price
end
