class PlayersController < ApplicationController

	def index
		#@players = Player.all
		@q = Player.all.ransack(params[:q])
		@players = @q.result.paginate(:page => params[:page], :per_page => 20)
	end

	def show
		@player = Player.find(params[:id])
		rescue ActiveRecord::RecordNotFound
			redirect_to players_path and return
	end
	
end
