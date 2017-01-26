class PlayersController < ApplicationController

	def index
		@players = Player.all
	end

	def show
		@player = Player.find(params[:id])
		rescue ActiveRecord::RecordNotFound
			redirect_to players_path and return
	end
	
end
