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
	
	def winratio_percentage winratio
		number_to_percentage(winratio * 100, precision: 2)
	end
	
	def css_class_by_wn8 wn8
		if (wn8 < 300)
			return "dred";  # bad
		elsif (wn8 < 450)
			return "red";   # bad
		elsif (wn8 < 650)
			return "orange";   # below average
		elsif (wn8 < 900)
			return "yellow";   # average
		elsif (wn8 < 1200)
			return "green";   # above average
		elsif (wn8 < 1600)
			return "dgreen";    # good
		elsif (wn8 < 2000)
			return "blue";    # very good
		elsif (wn8 < 2450)
			return "dblue";    # great
		elsif (wn8 < 2900)
			return "purple";    # unicum
		else
			return "dpurple";    # super_unicum
		end
	end
	
	def css_class_by_winrate winrate
		if (winrate < 0.46)
			return "dred";  # bad
		elsif (winrate < 0.47)
			return "red";   # bad
		elsif (winrate < 0.48)
			return "#orange";   # below average
		elsif (winrate < 0.50)
			return "yellow";   # average
		elsif (winrate < 0.52)
			return "green";   # above average
		elsif (winrate < 0.54)
			return "dgreen";    # good
		elsif (winrate < 0.56)
			return "blue";    # very good
		elsif (winrate < 0.60)
			return "dblue";    # great
		elsif (winrate < 0.65)
			return "purple";    # unicum
		else
			return "dpurple";    # super_unicum
		end
	end

end
