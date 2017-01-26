module ApplicationHelper

	def player_url_wg wgid
		"http://worldoftanks.eu/en/community/accounts/" + wgid.to_s
	end
	
	def player_url_wotlabs name
		"http://wotlabs.net/eu/player/" +  name
	end
	
	def clan_url_wg wgid
		"http://eu.wargaming.net/clans/wot/" + wgid.to_s
	end
	
	def clan_url_wotlabs tag
		"http://wotlabs.net/eu/clan/" + tag
	end
	
	def wn8_colorcode wn8
		if (wn8 < 300)
			return "#930d0d";  # bad
		elsif (wn8 < 450)
			return "#f11919";   # bad
		elsif (wn8 < 650)
			return "#ff8a00";   # below average
		elsif (wn8 < 900)
			return "#e6df27";   # average
		elsif (wn8 < 1200)
			return "#77e812";   # above average
		elsif (wn8 < 1600)
			return "#459300";    # good
		elsif (wn8 < 2000)
			return "#2ae4ff";    # very good
		elsif (wn8 < 2450)
			return "#00a0b8";    # great
		elsif (wn8 < 2900)
			return "#c64cff";    # unicum
		else
			return "#8225ad";    # super_unicum
		end
	end
	
	def wn8_foreground_colorcode wn8
		if (wn8 >= 1600 && wn8 < 2000) || (wn8 >= 650 || wn8 < 1200)
			return "#FFFFFF"
		else
			return "#000000"
		end
	end
	
	def winratio_colorcode winrate
		if (winrate < 0.46)
			return "#930d0d";  # bad
		elsif (winrate < 0.47)
			return "#f11919";   # bad
		elsif (winrate < 0.48)
			return "#ff8a00";   # below average
		elsif (winrate < 0.50)
			return "#e6df27";   # average
		elsif (winrate < 0.52)
			return "#77e812";   # above average
		elsif (winrate < 0.54)
			return "#459300";    # good
		elsif (winrate < 0.56)
			return "#2ae4ff";    # very good
		elsif (winrate < 0.60)
			return "#00a0b8";    # great
		elsif (winrate < 0.65)
			return "#c64cff";    # unicum
		else
			return "#8225ad";    # super_unicum
		end
	end

	def winratio_foreground_colorcode winrate
		if (winrate >= 0.54 && winrate < 0.56) || (winrate >= 0.48 || winrate < 0.52)
			return "#FFFFFF"
		else
			return "#000000"
		end
	end
end
