local wm = GetWindowManager()

function Zen:CreateWindow()
	local scrW = GuiRoot:GetWidth()
	local scrH = GuiRoot:GetHeight()

	if not self.prefs.winX or self.prefs.winX > scrW - 48 then self.prefs.winX = scrW - 48 end
	if not self.prefs.winY or self.prefs.winY > scrH - 48 then self.prefs.winY = scrH - 48 end

	self.window = wm:CreateTopLevelWindow("ZenWindow")
	local win = self.window
	win:SetAnchor(TOPLEFT, GuiRoot, TOPLEFT, self.prefs.winX, self.prefs.winY)
	win:SetMovable(true)
	win:SetHidden(not self.prefs.showWindow)
	win:SetMouseEnabled(true)
	win:SetClampedToScreen(true)
	win:SetDimensions(48, 48)
	win:SetResizeToFitDescendents(true)
	win:SetHandler("OnMoveStop", function()
		self.prefs.winX = Zen.window:GetLeft()
		self.prefs.winY = Zen.window:GetTop()
	end)

	win.icon = wm:CreateControl("ZenIcon", win, CT_TEXTURE)
	win.icon:SetAnchor(TOPLEFT, win, TOPLEFT, 0, 0)
	win.icon:SetHidden(false)
	win.icon:SetDimensions(48, 48)

	win.overlay = wm:CreateControl("ZenOverlay", win, CT_TEXTURE)
	win.overlay:SetAnchor(TOPLEFT, win, TOPLEFT, 0, 0)
	win.overlay:SetHidden(false)
	win.overlay:SetDimensions(48, 48)
	win.overlay:SetTexture("Zenithar/media/zenithar_star_active.dds")

	self:UpdateWindow()

	if ZO_CompassFrame:IsHandlerSet("OnShow") then
		local oldHandler = ZO_CompassFrame:GetHandler("OnShow")
		ZO_CompassFrame:SetHandler("OnShow", function(...) oldHandler(...) if Zen.prefs.showWindow then Zen.window:SetHidden(false) end end)
	else
		ZO_CompassFrame:SetHandler("OnShow", function(...) if Zen.prefs.showWindow then Zen.window:SetHidden(false) end end)
	end
	if ZO_CompassFrame:IsHandlerSet("OnHide") then
		local oldHandler = ZO_CompassFrame:GetHandler("OnHide")
		ZO_CompassFrame:SetHandler("OnHide", function(...) oldHandler(...) if Zen.prefs.showWindow then Zen.window:SetHidden(true) end end)
	else
		ZO_CompassFrame:SetHandler("OnHide", function(...) if Zen.prefs.showWindow then Zen.window:SetHidden(true) end end)
	end

end

function Zen:UpdateWindow()
	self.window.icon:SetTexture(self.lastEvent and "Zenithar/media/zenithar_star_data.dds" or "Zenithar/media/zenithar_star_inactive.dds")

	if self.lastEvent then
		local currentAlpha = 1.0 - (GetFrameTimeMilliseconds() - self.lastEvent) / 1000.0
		self.window.overlay:SetAlpha(currentAlpha >= 0.0 and currentAlpha or 0.0)
		if currentAlpha > 0.0 then
			zo_callLater(function() Zen:UpdateWindow() end, 4)
		end
	else
		self.window.overlay:SetAlpha(0.0)
	end
end