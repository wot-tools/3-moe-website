class ClansController < ApplicationController

	def index
		#@clans = Clan.all
		@q = Clan.all.ransack(params[:q])
		@clans = @q.result.paginate(:page => params[:page], :per_page => 20)
	end

	def show
		@clan = Clan.find(params[:id])
		rescue ActiveRecord::RecordNotFound
			redirect_to clans_path and return
	end
end
