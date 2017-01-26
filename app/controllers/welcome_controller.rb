class WelcomeController < ApplicationController
  def index
	@clans = Clan.all
	@tanks = Tank.all
	@players = Player.all
	@marks = Mark.all
  end
end
