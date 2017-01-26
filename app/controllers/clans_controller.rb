class ClansController < ApplicationController

	def index
		@clans = Clan.all
	end

	def show
		@clan = Clan.find(params[:id])
		rescue ActiveRecord::RecordNotFound
			redirect_to clans_path and return
	end
end
