class PlayersController < ApplicationController

	def index
		@q = Player.all.ransack(params[:q])
		@players = @q.result.paginate(:page => params[:page], :per_page => 20)
	end

	def show
		@player = Player.find(params[:id])
		@marks = @player.marks.paginate(:page => params[:page], :per_page => 20)
		
		rescue ActiveRecord::RecordNotFound
			redirect_to players_path and return
	end
	
end
