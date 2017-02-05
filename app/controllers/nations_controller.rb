class NationsController < ApplicationController

	def index
		@q = Nation.distinct.ransack(params[:q])
		@nations = @q.result.paginate(:page => params[:page], :per_page => 20)
	end
	
	def show
		@nation = Nation.find(params[:id])
		@tanks = @nation.tanks.paginate(:page => params[:page], :per_page => 20)
		
		rescue ActiveRecord::RecordNotFound
			redirect_to nations_path and return
	end

end
