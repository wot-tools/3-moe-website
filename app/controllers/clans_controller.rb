class ClansController < ApplicationController

	def index
		@q = Clan.all.ransack(params[:q])
		@clans = @q.result.paginate(:page => params[:page], :per_page => 20)
	end

	def show
		@clan = Clan.find(params[:id])
		
		@q = @clan.players.ransack(params[:q])
		@players = @q.result.distinct.paginate(:page => params[:page], :per_page => 20)
		
		rescue ActiveRecord::RecordNotFound
			redirect_to clans_path and return
	end
end
