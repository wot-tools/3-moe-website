class TiersController < ApplicationController

	def index
		@q = Tier.distinct.ransack(params[:q])
		@tiers = @q.result.paginate(:page => params[:page], :per_page => 20)
	end
	
	def show
		@tier = Tier.find(params[:id])
		
		@q = @tier.tanks.ransack(params[:q])
		@tanks = @q.result.paginate(:page => params[:page], :per_page => 20)
		
		rescue ActiveRecord::RecordNotFound
			redirect_to tiers_path and return
	end

end
