class PlayersController < ApplicationController

	def index
		@q = Player.all.ransack(params[:q])
		@players = @q.result.includes(:clan).paginate(:page => params[:page], :per_page => 20)
	end

	def show
		@player = Player.find(params[:id])

		@q = @player.marks.ransack(params[:q])
		@marks = @q.result.includes(:tank, tank: [:nation, :vehicle_type]).paginate(:page => params[:page], :per_page => 20)
		
		rescue ActiveRecord::RecordNotFound
			redirect_to players_path and return
	end
	
end
